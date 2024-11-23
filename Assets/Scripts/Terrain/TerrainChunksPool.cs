using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public class TerrainChunksPool : MonoBehaviour
	{
		private readonly Dictionary<Vector3Int, TerrainChunk> currentChunks = new Dictionary<Vector3Int, TerrainChunk>();
		private readonly Queue<TerrainChunk> deactivatedChunks = new Queue<TerrainChunk>();

		public TerrainChunk Instantiate(Vector3Int chunkIndex, Transform parent)
		{
			TerrainChunk newChunk;

			if (deactivatedChunks.Count == 0)
			{
				var chunk = new GameObject("CHUNK", typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider));
				chunk.transform.SetParent(parent);
				newChunk = chunk.AddComponent<TerrainChunk>();
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

		public void DisposeAll(Vector3Int exceptIndex)
		{
			foreach (var key in currentChunks)
			{
				if (key.Key == exceptIndex) continue;
				if(!currentChunks[key.Key]) continue;
				Destroy(currentChunks[key.Key].gameObject);
			}

			for (int i = 0; i < deactivatedChunks.Count; i++)
			{
				var chunk = deactivatedChunks.Dequeue();
				Destroy(chunk.gameObject);
			}
			currentChunks.Clear();
			deactivatedChunks.Clear();
		}

		public TerrainChunk GetChunk(Vector3Int chunkIndex)
		{
			return currentChunks.GetValueOrDefault(chunkIndex);
		}
	}
}