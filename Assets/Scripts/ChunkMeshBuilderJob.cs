using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

namespace Minecraft
{
	// https://www.jacksondunstan.com/articles/5397
	public class ManagedObjectWorld
    {
		private int m_NextId;
		private readonly Dictionary<int, object> m_Objects;

		public ManagedObjectWorld(int initialCapacity = 1000)
        {
			m_NextId = 1;
			m_Objects = new Dictionary<int, object>(initialCapacity);
        }

		public ManagedObjectReference<T> Add<T>(T obj) where T : class
        {
			int id = m_NextId;
			m_NextId++;
			m_Objects[id] = obj;
			return new ManagedObjectReference<T>(id);
        }

		public T Get<T>(ManagedObjectReference<T> objRef) where T : class
        {
			return (T)m_Objects[objRef.Id];
        }

		public void Remove<T>(ManagedObjectReference<T> objRef) where T : class
        {
			m_Objects.Remove(objRef.Id);
        }
    }

	public struct ManagedObjectReference<T> where T : class
    {
		public readonly int Id;

		public ManagedObjectReference(int id) => Id = id;
    }

	[BurstCompile]
	public struct TerrainChunkMeshBuildJob : IJob
	{
		public NativeList<Vector3> vertices;
		public NativeList<int> triangles;
		public NativeList<Vector3> uvs; //TODO: UV

		public NativeArray<TerrainBlock> blocks;
		public ManagedObjectReference<Material> materialData;
		public NativeArray<float> maskedBlockTypes;
		public int3 chunkSize;

		private const int SOUTH = 0, NORTH = 1,
				EAST = 2, WEST = 3, TOP = 4, BOTTOM = 5;

		public void Execute()
		{
			// Mostly based on a Greedy Me;shing algorithm by Mikola Lysenko and code based on Cleo McCoy (formely Rob O'Leary)
			var emptyBlock = new TerrainBlock(BlockType.Air, Vector3Int.zero, Vector3Int.zero, 0);

			for (bool backFace = true, b = false; b != backFace; backFace = backFace && b, b = !b)
			{
				// Cycle through all 3 axis
				for (int d = 0; d < 3; d++)
				{
					int i, j, k, l, w, h;
					int u = (d + 1) % 3; // 
					int v = (d + 2) % 3; // 
					var x = new int3();
					var q = new int3();
					int side = 0;

					switch (d)
					{
						case 0:
							side = backFace ? WEST : EAST;
							break;
						case 1:
							side = backFace ? BOTTOM : TOP;
							break;
						case 2:
							side = backFace ? SOUTH : NORTH;
							break;
					}

					var mask = new NativeArray<TerrainBlock>(chunkSize[u] * chunkSize[v], Allocator.Temp);
					q[d] = 1;

					// Check each slice of the chunk one at a time
					for (x[d] = -1; x[d] < chunkSize[d];)
					{
						// Compute the mask
						var n = 0;
						for (x[v] = 0; x[v] < chunkSize[v]; ++x[v])
						{
							for (x[u] = 0; x[u] < chunkSize[u]; ++x[u])
							{
								// q determines the direction (X, Y or Z) that we are searching
								// m.IsBlockAt(x,y,z) takes chunk positions and returns true if a block exists there

								// Apparently this avoids blocks at chunk borders being ignored
								emptyBlock.index = MMMath.FloorToInt3D(x[0] / chunkSize.x, x[1] / chunkSize.y, x[2] / chunkSize.z);
								emptyBlock.globalIndex = MMMath.FloorToInt3D(x[0], x[1], x[2]);
								TerrainBlock blockCurrent = (x[d] >= 0) ? blocks[MMMath.FlattenIndex(x[0], x[1], x[2], chunkSize.x, chunkSize.y)] : emptyBlock;
								TerrainBlock blockCompare = (x[d] < chunkSize[d] - 1) ? blocks[MMMath.FlattenIndex(x[0] + q[0], x[1] + q[1], x[2] + q[2], chunkSize.x, chunkSize.y)] : emptyBlock;
								//manager.GetBlockTypeAt(chunk, (chunk.index * chunkSize) + x) : emptyBlock;
								//manager.GetBlockTypeAt(chunk, (chunk.index * chunkSize) + x + q) : emptyBlock;

								blockCurrent.metadata = (byte)(7 >> side);
								blockCompare.metadata = (byte)(7 >> side);

								mask[n++] = (!blockCurrent.IsEmpty() && !blockCompare.IsEmpty() && blockCurrent.Equals(blockCompare))
									? emptyBlock : backFace ? blockCompare : blockCurrent;
							}
						}

						x[d]++;

						n = 0;

						// Generate a mesh from the mask using lexicographic ordering,      
						//   by looping over each block in this slice of the chunk
						for (j = 0; j < chunkSize[v]; j++)
						{
							for (i = 0; i < chunkSize[u];)
							{
								if (!mask[n].IsEmpty())
								{
									// Compute the width of this quad and store it in w                        
									//   This is done by searching along the current axis until mask[n + w] is false
									for (w = 1; i + w < chunkSize[u] && !mask[n + w].IsEmpty() && mask[n+w].metadata == mask[n].metadata; w++) { }

									// Compute the height of this quad and store it in h                        
									//   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
									//   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
									//   greedy meshing will attempt to expand this quad out to CHUNK_SIZE x 5, but will stop if it reaches a hole in the mask

									var done = false;
									for (h = 1; j + h < chunkSize[v]; h++)
									{
										// Check each block next to this quad
										for (k = 0; k < w; ++k)
										{
											// If there's a hole in the mask, exit
											if (mask[n + k + h * chunkSize[u]].IsEmpty())
											{
												done = true;
												break;
											}
										}
										if (done)
											break;
									}

									x[u] = i;
									x[v] = j;

									// du and dv determine the size and orientation of this face
									var du = new int3();
									du[u] = w;

									var dv = new int3();
									dv[v] = h;

									var blockMinVertex = new float3(1f, 1f, 1f) * -0.5f;

									//if (d != 0 && (i != 0 || i != chunkSize[u]))
									CreateQuad(
										blockMinVertex + x,
										blockMinVertex + x + du,
										blockMinVertex + x + du + dv,
										blockMinVertex + x + dv,
										w,
										h,
										backFace,
										mask[n],
										side
										);

									// Clear this part of the mask, so we don't add duplicate faces
									for (l = 0; l < h; ++l)
										for (k = 0; k < w; ++k)
											mask[n + k + l * chunkSize[u]] = emptyBlock;

									// Increment counters and continue
									i += w;
									n += w;
								}
								else
								{
									i++;
									n++;
								}
							}
						}
					}
				}
			}
		}
		internal NativeArray<float3> GetUVCoordinates(int uOffset, int vOffset)
		{
			const int textureSize = 128;
			const int tileSize = 32;
			const float offsetFix = 0.001f;

			int u = uOffset * tileSize;
			int v = vOffset * tileSize;

			var result = new NativeArray<float3>(4, Allocator.Temp);
			result[0] = new float3(u / textureSize + offsetFix, v / textureSize + offsetFix, 0);
			result[1] = new float3(u / textureSize + offsetFix, (v + tileSize) / textureSize - offsetFix, 0);
			result[2] = new float3((u + tileSize) / textureSize - offsetFix, (v + tileSize) / textureSize - offsetFix, 0);
			result[3] = new float3((u + tileSize) / textureSize - offsetFix, v / textureSize + offsetFix, 0);
			return result;
		}

		public void CreateQuad(
				float3 bottomLeft,
				float3 topLeft,
				float3 topRight,
				float3 bottomRight,
				float width,
				float height,
				bool backFace,
				TerrainBlock mask,
				int side
				)
		{
			if (backFace)
			{
				triangles.Add(vertices.Length + 2); // 1
				triangles.Add(vertices.Length + 0); // 2
				triangles.Add(vertices.Length + 1);

				triangles.Add(vertices.Length + 1); // 1
				triangles.Add(vertices.Length + 3); // 3
				triangles.Add(vertices.Length + 2); // 2
			}
			else
			{
				triangles.Add(vertices.Length + 2);
				triangles.Add(vertices.Length + 3); // 2
				triangles.Add(vertices.Length + 1); // 1

				triangles.Add(vertices.Length + 1); // 2
				triangles.Add(vertices.Length + 0); // 3
				triangles.Add(vertices.Length + 2); // 1
			}

			/*float3 size = new float3(width, height, 0f);
			bottomRight += size;
			topRight.x += size.x;*/

			vertices.Add(bottomLeft);
			vertices.Add(bottomRight);
			vertices.Add(topLeft);
			vertices.Add(topRight);

			//float[] types = new float[mask.Length];
			/*maskedBlockTypes = new NativeArray<float>(mask.Length, Allocator.Temp);
            for (int i = 0; i < mask.Length; i++)
            {
				maskedBlockTypes[i] = (int)mask[i].type;
            }*/
			//Debug.Log($"Side: {side}; Block: {(mask[0].type)}");
			AddUVForSide(side, mask.type);
		}

		public void AddUVForSide(int side, BlockType block)
		{
			//Debug.Log($"Side: {side}; Block: {((int)block)}");
			//if (block == BlockType.Air) return;
			int u = 0;
			int v = 0;
			int w = 0;
			if (block == BlockType.Air)
			{
				w = 13;
			}
			if (block == BlockType.Grass)
			{
				switch (side)
				{
					case NORTH:
					case SOUTH:
					case WEST:
					case EAST:
						w = 8;
						//u = 0;
						//v = 1;
						break;
					case TOP:
						w = 13;
						//u = 1;
						//v = 0;
						break;
					case BOTTOM:
						w = 12;
						//u = 0;
						//v = 0;
						break;
					default:
						w = 12;
						break;
				}
			}
			else if (block == BlockType.Stone)
			{
				w = 15;
			}
			else if (block == BlockType.Dirt)
			{
				w = 12;
			}
			uvs.Add(new Vector3(0, 0, w));
			uvs.Add(new Vector3(1f, 0, w));
			uvs.Add(new Vector3(0, 1f, w));
			uvs.Add(new Vector3(1f, 1f, w));

			//var uvCoords = GetUVCoordinates(u, v);
			//for (int i = 0; i < uvCoords.Length; i++)
			//{
			//	uvs.Add(uvCoords[i]);
			//}
			//uvCoords.Dispose();
		}
	}
}