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
		public TerrainGeneratorBase terrainGenerator;
		public UnityEngine.ThreadPriority chunkGeneratePriority = UnityEngine.ThreadPriority.Low;
		public GameObject playerPrefab;
		public Material defaultMaterial;
		public TerrainChunksPool chunksPool;
		public Vector3Int chunkSize;
		public uint renderDistance = 12U;

		private Transform player;
		private uint prevRenderDistance;
		public float _RayOffset = 0.01f;
		private Vector3Int playerChunk = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
		private bool playerSpawned = false;
		private readonly HashSet<Vector3Int> currentChunks = new HashSet<Vector3Int>();
		private bool updatingTerrain = false;
		private bool forceUpdate = true;
		private ChunkMeshBuilder builder;

		private void OnDisable()
		{
			builder.Dispose();
		}

		private void Awake()
		{
			if (!int.TryParse(seed, out int numSeed))
				numSeed = seed.GetHashCode();
			

			Debug.Assert(terrainGenerator != null);
			terrainGenerator.SetSeed(numSeed);		

			builder = new ChunkMeshBuilder(this);
			player = playerPrefab.transform;
			playerChunk = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

			// Sometimes there's an error but no exception thrown due to how Unity and Tasks work
			TerrainChunk debugChunk = chunksPool.Instantiate(Vector3Int.zero, transform);
			debugChunk.Setup(Vector3Int.zero, chunkSize);
			BuildMeshForChunk(debugChunk);
		}

		// Start is called before the first frame update
		private async void Start()
		{	
			forceUpdate = true;
			await UpdateTerrain().ContinueWith(_ => Debug.Log("First load finished"));
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


        private async void Update()
		{
			if (prevRenderDistance != renderDistance && renderDistance != 0 /* To fix chunks never generating again*/)
			{
				chunksPool.DisposeAll(playerChunk);
				forceUpdate = true;
				updatingTerrain = false;
				prevRenderDistance = renderDistance;
			}

			Application.backgroundLoadingPriority = chunkGeneratePriority;
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
							// for (int y = -(int)renderDistance; y <= playerChunk.y + renderDistance; y++)
							// {
								for (int z = playerChunk.z - (int)renderDistance; z <= playerChunk.z + (int)renderDistance; z++)
								{
									yield return new Vector3Int(x, 0, z);
								}
							// }
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
				newChunk.Setup(chunkIndex, chunkSize, false);
				newChunk.transform.position = new Vector3Int(newChunk.index.x, newChunk.index.y, newChunk.index.z) * newChunk.Size;

				terrainGenerator.GenerateBlocksFor(newChunk);

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

				SetBlockTypeAt(chunk, block.Value.index, blockType);
				BuildMeshForChunk(chunk);
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
				BuildMeshForChunk(chunk);
			}

			return block;
		}

		public TerrainBlock? GetBlockAt(Vector3 worldPoint, out TerrainChunk chunk)
		{
			var chunkIndex = GetChunkIndexAt(worldPoint);
			chunk = chunksPool.GetChunk(chunkIndex);

			// TODO: Use TerrainChunkGenerator GetOrGenerate
			if (chunk == null)
			{
				return null;
				chunk = chunksPool.Instantiate(chunkIndex, transform);
				chunk.transform.position = chunkIndex * chunkSize;
				terrainGenerator.GenerateBlocksFor(chunk);
				BuildMeshForChunk(chunk);
			}
				
			return chunk.GetBlock(
				MMMath.FloorToInt3D(worldPoint - (chunk.index * chunk.Size) + Vector3.one * 0.5f)
				);
		}

		public TerrainBlock? GetBlockAt(Vector3 worldPoint)
		{
			return GetBlockAt(worldPoint, out _);
		}

		public void SetBlockTypeAt(TerrainChunk chunk, Vector3Int localBlockIndex, BlockType type, bool regenerateMesh = false)
		{
			Debug.Assert(chunk != null, "Chunk must not be null");
			chunk.SetBlockType(localBlockIndex, type);

			if (!regenerateMesh) return;
			BuildMeshForChunk(chunk);
		}

		public void BuildMeshForChunk(TerrainChunk chunk)
		{
			builder.Clear();
			Mesh m = builder.JobsBuildChunk(chunk);
			chunk.SetMesh(m, null);
		}

		private Vector3Int GetChunkIndexAt(Vector3 pointInWorld)
		{
			return Vector3Int.FloorToInt(new Vector3(pointInWorld.x / chunkSize.x, pointInWorld.y / chunkSize.y, pointInWorld.z / chunkSize.z));
		}
	}
}