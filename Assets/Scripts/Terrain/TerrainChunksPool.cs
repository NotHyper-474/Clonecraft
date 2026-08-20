using System.Collections.Generic;
using UnityEngine;

namespace Clonecraft
{
    public class TerrainChunksPool : MonoBehaviour
    {
        [SerializeField] private TerrainChunk chunkPrefab;

        private readonly Dictionary<Vector3Int, TerrainChunk> _currentChunks = new();
        private readonly Queue<TerrainChunk> _deactivatedChunks = new();

        public TerrainChunk Instantiate(Vector3Int chunkIndex, Transform parent, bool reactivate = true)
        {
            if (_deactivatedChunks.TryDequeue(out var newChunk))
            {
                if (reactivate)
                    newChunk.gameObject.SetActive(true);
            }
            else
            {
                newChunk = Instantiate(chunkPrefab, parent);
            }
            
            _currentChunks[chunkIndex] = newChunk;

            //Debug.Log("Chunk Count: " + _currentChunks.Count);
            //Debug.Log("Deactivated Chunk Count: " + _deactivatedChunks.Count);

            return newChunk;
        }

        public void Deactivate(IEnumerable<Vector3Int> chunksToDestroy)
        {
            foreach (var chunkIndex in chunksToDestroy)
            {
                var chunk = GetChunk(chunkIndex);
                if (!chunk) continue;
                chunk.gameObject.SetActive(false);
                _deactivatedChunks.Enqueue(chunk);
                _currentChunks.Remove(chunk.Index);
            }
        }

        public void DisposeAll(Vector3Int? exceptIndex = null)
        {
            TerrainChunk exceptChunk = null;
            foreach (var (key, chunk) in _currentChunks)
            {
                if (key == exceptIndex)
                {
                    exceptChunk = chunk;
                    continue;
                }
                if (chunk)
                    Destroy(chunk.gameObject);
            }
            
            _currentChunks.Clear();
            if (exceptChunk)
            {
                _currentChunks[exceptIndex.Value] = exceptChunk;
            }

            while (_deactivatedChunks.TryPeek(out var chunk))
            {
                if (chunk.Index == exceptIndex)
                    continue;

                Destroy(chunk.gameObject);
                _deactivatedChunks.Dequeue();
            }
        }

        public TerrainChunk GetChunk(Vector3Int chunkIndex)
        {
            return _currentChunks.GetValueOrDefault(chunkIndex, null);
        }
    }
}