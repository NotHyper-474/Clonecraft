using System.Threading.Tasks;
using UnityEngine;

namespace Minecraft
{
    public abstract class TerrainGeneratorBase : ScriptableObject, ITerrainGenerator
    {
        protected readonly FastNoise noise = new FastNoise();

        public virtual void SetSeed(int seed)
        {
            noise.SetSeed(seed);
        }

        public abstract VoxelType CalculateBlockType(Vector3Int chunkSize, Vector3Int globalIndex);
        public virtual void GenerateBlocksFor(TerrainChunk chunk)
        {
            Parallel.For (0, chunk.Blocks.Length, (i) =>
            {
                var j = MathUtils.To3D(i, chunk.Size.x, chunk.Size.y);
                var globalIndex = j + chunk.Index * chunk.Size;
                chunk.Blocks[i].index = j;
                chunk.Blocks[i].globalIndex = globalIndex;
                chunk.Blocks[i].type = CalculateBlockType(chunk.Size, globalIndex);
            });
        }
    }
}