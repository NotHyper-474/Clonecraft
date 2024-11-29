using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minecraft
{
	public class TerrainChunksPool : MonoBehaviour
	{
		[SerializeField] private TerrainChunk chunkPrefab;
		
		private readonly Dictionary<Vector3Int, TerrainChunk> currentChunks = new Dictionary<Vector3Int, TerrainChunk>();
		private readonly Queue<TerrainChunk> deactivatedChunks = new Queue<TerrainChunk>();

		// ReSharper disable Unity.PerformanceAnalysis
		public TerrainChunk Instantiate(Vector3Int chunkIndex, Transform parent)
		{
			TerrainChunk newChunk;

			if (deactivatedChunks.Count == 0)
			{
				newChunk = Instantiate(chunkPrefab, parent);
			}
			else
			{
				newChunk = deactivatedChunks.Dequeue();
				newChunk.SetMesh(null, null);
				newChunk.gameObject.SetActive(true);
			}

			currentChunks[chunkIndex] = newChunk;

			return newChunk;
		}

		public void Deactivate(IEnumerable<Vector3Int> chunksToDestroy)
		{
			foreach (var chunkIndex in chunksToDestroy)
			{
				var chunk = GetChunk(chunkIndex);
				if (!chunk) continue;
				chunk.gameObject.SetActive(false);
				deactivatedChunks.Enqueue(chunk);
			}
		}

		public void DisposeAll(Vector3Int? exceptIndex = null)
		{
			foreach (var (key, chunk) in currentChunks)
			{
				if (key == exceptIndex) continue;
				if(!chunk) continue;
				Destroy(chunk.gameObject);
			}

			for (int i = 0; i < deactivatedChunks.Count; i++)
			{
				var chunk = deactivatedChunks.Dequeue();
				Destroy(chunk.gameObject);
			}
			
			deactivatedChunks.Clear();
		}

		public TerrainChunk GetChunk(Vector3Int chunkIndex)
		{
			return currentChunks.GetValueOrDefault(chunkIndex);
		}
	}
}