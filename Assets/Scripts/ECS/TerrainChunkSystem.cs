using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;

namespace Minecraft.DOTS
{
	[System.Serializable]
	public struct TerrainChunkData : IComponentData
	{
		public DynamicBuffer<TerrainBlock> blocks;
		public int3 Size;
		public int3 Index;

		public int Length
		{
			get => Size.x * Size.y * Size.z;
		}
	}

	public sealed class TerrainChunkSystem : ComponentSystem
	{
		public Mesh blockMesh;
		public Material blockMaterial;

		[Unity.Burst.BurstCompile]
		public struct BlockJob : IJobFor
		{
			/*public BlockJob(short ignoreThisVariable)
			{
				chunk = new TerrainChunkData();
				blocks = new NativeArray<TerrainBlock>(0, Allocator.TempJob);
				blockMesh = new MeshData();
			}*/

			[ReadOnly] public TerrainChunkData chunk;
			public NativeArray<TerrainBlock> blocks;
			public Mesh.MeshData blockMesh;

			public NativeList<Vector3> vertices;
			public NativeList<int> triangles;
			public NativeList<Vector2> uvs;

			public void Execute(int i)
			{
				var index = MMMath.To3D((long)i, chunk.Size.x, chunk.Size.y);
				//NativeArray<TerrainBlock> blocksContainer = new NativeArray<TerrainBlock>(blocks, Allocator.Temp);
				var block = blocks[i];
				block.index = index;
				block.globalIndex = index + (chunk.Index * chunk.Size);
				blocks[i] = block;

				var verts = new NativeArray<Vector3>(24, Allocator.Temp);
				var tris = new NativeArray<int>(36, Allocator.Temp);
				var uvs2 = new NativeArray<Vector2>(24, Allocator.Temp);
				blockMesh.GetVertices(verts);
				blockMesh.GetIndices(tris, 0);
				blockMesh.GetUVs(0, uvs2);

				for (int j = 0; j < verts.Length; j++)
					vertices.Add(verts[j]);
				for (int k = 0; k < tris.Length; k++)
					triangles.Add(tris[k]);
				for (int l = 0; l < uvs2.Length; l++)
					uvs.Add(uvs2[l]);

				verts.Dispose();
				tris.Dispose();
				uvs2.Dispose();
			}

			public void Dispose()
			{
				vertices.Dispose();
				triangles.Dispose();
				uvs.Dispose();
			}
		}

		protected override void OnUpdate()
		{
			BlockJob blockJob;
			Entities.ForEach((Entity entity, ref TerrainChunkData chunk) =>
			{
				blockJob = new BlockJob()
				{
					chunk = chunk,
					blocks = chunk.blocks.AsNativeArray(),
					//blockMesh = this.blockMesh // "this" for more code readability
				};
				JobHandle handle = blockJob.Schedule(chunk.Length, default);
				handle.Complete();

				var vertBuffer = EntityManager.AddBuffer<Vector3ElementData>(entity);
				var triBuffer = EntityManager.AddBuffer<IntElementData>(entity);
				var uvBuffer = EntityManager.AddBuffer<Vector2ElementData>(entity);

				var arr1 = new NativeArray<Vector3ElementData>(blockJob.vertices.Length, Allocator.Persistent);
				var arr2 = new NativeArray<IntElementData>(blockJob.triangles.Length, Allocator.Persistent);
				var arr3 = new NativeArray<Vector2ElementData>(blockJob.uvs.Length, Allocator.Persistent);

				vertBuffer.AddRange(arr1);
				triBuffer.AddRange(arr2);
				uvBuffer.AddRange(arr3);

				for (int i = 0; i < vertBuffer.Length; i++)
				{
					vertBuffer[i] = new Vector3ElementData() { Value = blockJob.vertices.ToArray()[i] };
				}
				for (int j = 0; j < triBuffer.Length; j++)
				{
					triBuffer[j] = new IntElementData() { Value = blockJob.triangles[j] };
				}
				for (int k = 0; k < uvBuffer.Length; k++)
				{
					uvBuffer[k] = new Vector2ElementData() { Value = blockJob.uvs[k] };
				}

				arr1.Dispose();
				arr2.Dispose();
				arr3.Dispose();
			});
		}
	}
}