using System;
using UnityEngine;

namespace Minecraft
{
    public sealed class TerrainChunkGenerator
    {
        public TerrainChunkGenerator(TerrainChunksPool chunksPool, TerrainConfig config)
        {
            _chunksPool = chunksPool;
            _config = config;
        }
        
        private readonly TerrainChunksPool _chunksPool;
        private readonly TerrainConfig _config;

        public TerrainChunk InstantiateAndSetup(Vector3Int index, Transform chunkParent)
        {
            var newChunk = _chunksPool.Instantiate(index, chunkParent);
            newChunk.Setup(index, _config, false);
            return newChunk;
        }

        public TerrainChunk GetOrGenerateChunk(Vector3Int chunkIndex, Transform chunkParent)
        {
            var chunk = _chunksPool.GetChunk(chunkIndex);
            if (!chunk) chunk = InstantiateAndSetup(chunkIndex, chunkParent);
            return chunk;
        }
    }
}