using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    public class TerrainChunksPool : MonoBehaviour
    {
        [SerializeField] private TerrainChunk chunkPrefab;

        private readonly Dictionary<Vector3Int, TerrainChunk> currentChunks = new();
        private readonly Queue<TerrainChunk> deactivatedChunks = new();
        
        public TerrainChunk Instantiate(Vector3Int chunkIndex, Transform parent, bool reactivate = true)
        {
            TerrainChunk newChunk;

            if (deactivatedChunks.Count == 0)
            {
                newChunk = Instantiate(chunkPrefab, parent);
            }
            else
            {
                newChunk = deactivatedChunks.Dequeue();
                if (reactivate)
                    newChunk.gameObject.SetActive(true);
            }
            
            currentChunks[chunkIndex] = newChunk;

            Debug.Log("Chunk Count: " + currentChunks.Count);
            Debug.Log("Deactivated Chunk Count: " + deactivatedChunks.Count);

            return newChunk;
        }

        public void Deactivate(IEnumerable<Vector3Int> chunksToDestroy)
        {
            foreach (var chunkIndex in chunksToDestroy)
            {
                var chunk = GetChunk(chunkIndex);
                if (!chunk) continue;
                chunk.gameObject.SetActive(false);
                //chunk.meshRenderer.enabled = false;
                deactivatedChunks.Enqueue(chunk);
                //currentChunks.Remove(chunkIndex);
            }
        }

        public void DisposeAll(Vector3Int? exceptIndex = null)
        {
            foreach (var (key, chunk) in currentChunks)
            {
                if (key == exceptIndex) continue;
                if (!chunk) continue;
                Destroy(chunk.gameObject);
            }

            for (var i = 0; i < deactivatedChunks.Count; i++)
            {
                var chunk = deactivatedChunks.Dequeue();
                if (chunk.Index == exceptIndex) continue;
                Destroy(chunk.gameObject);
            }

            currentChunks.Clear();
        }

        public TerrainChunk GetChunk(Vector3Int chunkIndex)
        {
            return currentChunks.GetValueOrDefault(chunkIndex, null);
        }
    }
}