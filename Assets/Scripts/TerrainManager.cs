using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace Minecraft
{
	public class TerrainManager : MonoBehaviour
	{
		public string seed;
		public Transform player;
		public Material defaultMaterial;
		public List<TerrainChunk> chunks;
		public TerrainChunksPool chunksPool;
		public Vector3Int chunkSize;
		public uint renderDistance = 12U;

		private FastNoise noise;
		private uint prevRenderDistance;
		public float _RayOffset = 0.01f;
		private Vector3Int playerChunk = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
		private readonly HashSet<Vector3Int> currentChunks = new HashSet<Vector3Int>();
		private bool updatingTerrain = false;
		private bool forceUpdate = true;
		private ChunkMeshBuilder builder;

		// Start is called before the first frame update
		private async void Start()
		{
			int numSeed = 1;
			if (!int.TryParse(seed, out numSeed))
				numSeed = seed.GetHashCode();
			noise = new FastNoise(numSeed);
			Debug.Log("Actual seed is " + noise.GetSeed());

			
			/*TerrainChunk chunk = TerrainChunk.SetupAndInstantiate(Vector3Int.zero, chunkSize, transform);
			TerrainChunk chunkGreedy = TerrainChunk.SetupAndInstantiate(Vector3Int.right, chunkSize, transform);
			chunks.Add(chunk);
			chunks.Add(chunkGreedy);

			builder = new ChunkMeshBuilder(this);

			for (int i = 0; i < chunks.Count; i++)
			{
				chunks[i].blocks[1].type = BlockType.Grass;
				chunks[i].blocks[17].type = BlockType.Grass;
				chunks[i].blocks[8].type = BlockType.Grass;
				chunks[i].blocks[10].type = BlockType.Grass;
				for (int j = 0; j < chunks[i].blocks.Length; j++)
				{
					chunks[i].blocks[j] = GetBlockTypeAt(chunks[i], (chunks[i].index * chunks[i].Size) + MMMath.To3D(j, chunks[i].Size.x, chunks[i].Size.y));
				}

				// Clear builder of pre-calculated vertices
				builder.Clear();
				Mesh chunkMesh = builder.JobsBuildChunk(chunks[i]);
				chunks[i].SetMesh(chunkMesh, defaultMaterial);
			}*/
			builder = new ChunkMeshBuilder(this);
			noise.SetNoiseType(FastNoise.NoiseType.Simplex);
			forceUpdate = true;
			playerChunk = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
			await UpdateTerrain().ContinueWith(_ => Debug.Log("First load finished"));
			playerChunk = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
		}

		private void OnDrawGizmos()
		{
			if (Application.isPlaying)
			{
				var pchunk = GetChunkIndexAt(player.position);
				if (pchunk != null)
					Gizmos.DrawWireCube(pchunk * chunkSize, chunkSize);
			}
		}

        private void OnGUI()
        {
            GUI.Label(new Rect(5f, Screen.height - 25f, 200f, 25f), $"Player.Y: {player.position.y}");
        }


        private async void LateUpdate()
		{
			if (prevRenderDistance != renderDistance && renderDistance != 0 /* To fix chunks never generating again*/)
			{
				chunksPool.DisposeAll(playerChunk);
				forceUpdate = true;
				updatingTerrain = false;
				prevRenderDistance = renderDistance;
			}

			Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.Low;
			await UpdateTerrain();
			Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.Normal;
		}

		private async Task UpdateTerrain()
		{
			if (!updatingTerrain)
			{
				var newPlayerChunk = GetChunkIndexAt(player.position);

				if (forceUpdate || (playerChunk - newPlayerChunk).sqrMagnitude > renderDistance / 1.5f)
				{
					IEnumerable<Vector3Int> ChunksAround()
					{
						for (int x = playerChunk.x - (int)renderDistance; x <= playerChunk.x + (int)renderDistance; x++)
						{
							for (int y = -(int)renderDistance; y <= playerChunk.y + renderDistance; y++)
							{
								for (int z = playerChunk.z - (int)renderDistance; z <= playerChunk.z + (int)renderDistance; z++)
								{
									yield return new Vector3Int(x, y, z);
								}
							}
						}
					}
					var newCurrentChunks = ChunksAround().Select(i => new Vector3Int(i.x, i.y, i.z));

					var chunksToDestroy = currentChunks.Except(newCurrentChunks);
					Vector3Int[] chunksToCreate = newCurrentChunks.Except(currentChunks).ToArray();
					Vector3Int aux = Vector3Int.one * int.MinValue;
					int idx = -1;
					for (int i = 0; i < chunksToCreate.Length; i++)
					{
						if (chunksToCreate[i] == new Vector3(playerChunk.x, 0f, playerChunk.z))
						{
							aux = chunksToCreate[i];
							idx = i;
							break;
						}
					}
					if (idx != -1)
					{
						chunksToCreate[idx] = chunksToCreate[0];
						chunksToCreate[0] = aux;
					}

					chunksPool.Deactivate(chunksToDestroy);

					updatingTerrain = true;

					await GenerateChunks(chunksToCreate).ContinueWith(_ =>
					{
						currentChunks.Clear();
						currentChunks.UnionWith(newCurrentChunks);
						playerChunk = newPlayerChunk;
						updatingTerrain = false;
						forceUpdate = false;
					});
				}
			}
		}

		private async Task GenerateChunks(IEnumerable<Vector3Int> chunksToCreate)
		{
			foreach (var chunkIndex in chunksToCreate)
			{
				var newChunk = chunksPool.Instantiate(chunkIndex, transform);
				newChunk.Setup(chunkIndex, chunkSize);
				newChunk.transform.position = new Vector3Int(newChunk.index.x, newChunk.index.y, newChunk.index.z) * newChunk.Size;

				Parallel.For(0, newChunk.blocks.Length, (int i) =>
				{
					newChunk.blocks[i] = GetBlockTypeAt(newChunk, newChunk.index * newChunk.Size + MMMath.To3D(i, chunkSize.x, chunkSize.y));
				});

				builder.Clear();
				Mesh m = builder.JobsBuildChunk(newChunk);
				newChunk.SetMesh(m, defaultMaterial);

				await Task.Yield();
			}
		}

		public PointOnTerrainMesh RaycastTerrainMesh(Ray ray, float offset, float maxDistance = 5f)
		{
			PointOnTerrainMesh result = null;

			if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
			{
				var point = hit.point + ray.direction * offset;
				result = new PointOnTerrainMesh(point, hit.normal, ray);
			}

			return result;
		}

		public TerrainBlock? AddBlock(Ray ray, BlockType blockType)
		{
			TerrainBlock? block = null;
			var pointOnTerrain = RaycastTerrainMesh(ray, -_RayOffset)?.Point;

			if (pointOnTerrain.HasValue)
			{
				block = GetBlockAt(pointOnTerrain.Value, out TerrainChunk chunk);

				if (block != null)
					SetBlockTypeAt(chunk, block.Value.index, blockType);
			}

			return block;
		}

		public TerrainBlock? RemoveBlock(Ray ray)
		{
			TerrainBlock? block = null;
			var pointOnTerrain = RaycastTerrainMesh(ray, _RayOffset)?.Point;

			if (pointOnTerrain.HasValue)
			{
				block = GetBlockAt(pointOnTerrain.Value, out TerrainChunk chunk);

				SetBlockTypeAt(chunk, block.Value.index, BlockType.Air);
			}

			return block;
		}

		public TerrainBlock? GetBlockAt(Vector3 worldPoint, out TerrainChunk chunk)
		{
			var chunkIndex = GetChunkIndexAt(worldPoint);
			chunk = chunksPool.GetChunk(chunkIndex);

			if (chunk != null)
			{
				return chunk.GetBlock(
					MMMath.FloorToInt3D(worldPoint - (chunk.index * chunk.Size) + Vector3.one * 0.5f)
					);
			}
			return null;
		}

		public TerrainBlock? GetBlockAt(Vector3 worldPoint)
		{
			return GetBlockAt(worldPoint, out _);
		}

		public void SetBlockTypeAt(TerrainChunk chunk, Vector3Int localBlockIndex, BlockType type)
		{
			chunk.SetBlockType(localBlockIndex, type);

			builder.Clear();
			var m = builder.JobsBuildChunk(chunk);
			chunk.SetMesh(m, null);
		}

		public void BuildMeshForChunk(TerrainChunk chunk)
		{
			builder.Clear();
			Mesh m = builder.JobsBuildChunk(chunk);
			chunk.meshFilter.mesh = m;
			chunk.meshCollider.sharedMesh = m;
		}

		private Vector3Int GetChunkIndexAt(Vector3 pointInWorld)
		{
			return Vector3Int.FloorToInt(new Vector3(pointInWorld.x / chunkSize.x, pointInWorld.y / chunkSize.y, pointInWorld.z / chunkSize.z));
		}

		// Totally not stolen from Sam Hogan (check him out he does some cool stuff)
		public TerrainBlock GetBlockTypeAt(TerrainChunk chunk, Vector3Int blockGlobalIndex)
		{
			int x, y, z;
			(x, y, z) = (blockGlobalIndex.x, blockGlobalIndex.y, blockGlobalIndex.z);


			float simplex1 = noise.GetSimplex(x * .8f, z * .8f) * 10;
			float simplex2 = noise.GetSimplex(x * 3f, z * 3f) * 10 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

			float heightMap = simplex1 + simplex2;

			//add the 2d noise to the middle of the terrain chunk
			float baseLandHeight = chunk.Size.y * .5f + heightMap;

			//3d noise for caves and overhangs and such
			float caveNoise1 = noise.GetPerlinFractal(x * 5f, y * 10f, z * 5f);
			float caveMask = noise.GetSimplex(x * .3f, z * .3f) + .3f;

			//stone layer heightmap
			float simplexStone1 = noise.GetSimplex(x * 1f, z * 1f) * 10;
			float simplexStone2 = (noise.GetSimplex(x * 5f, z * 5f) + .5f) * 20 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

			float stoneHeightMap = simplexStone1 + simplexStone2;
			float baseStoneHeight = chunk.Size.y * .25f + stoneHeightMap;


			//float cliffThing = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
			//float cliffThingMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;

			BlockType blockType = BlockType.Air;

			//under the surface, dirt block
			if (y <= baseLandHeight)
			{
				blockType = BlockType.Dirt;

				//just on the surface, use a grass type
				if (y > baseLandHeight - 1 && y > /*WaterChunk.waterHeight*/ 28 - 2)
					blockType = BlockType.Grass;

				if (y <= baseStoneHeight)
					blockType = BlockType.Stone;
			}


			if (caveNoise1 > Mathf.Max(caveMask, .2f))
				blockType = BlockType.Air;

			/*if(blockType != BlockType.Air)
				blockType = BlockType.Stone;*/

			//if(blockType == BlockType.Air && noise.GetSimplex(x * 4f, y * 4f, z*4f) < 0)
			//  blockType = BlockType.Dirt;

			//if(Mathf.PerlinNoise(x * .1f, z * .1f) * 10 + y < TerrainChunk.chunkHeight * .5f)
			//    return BlockType.Grass;

			return new TerrainBlock(blockType, new Vector3Int(x / chunkSize.x, y / chunkSize.y, z / chunkSize.z), blockGlobalIndex, 0);
		}
	}
}