using System.Threading.Tasks;
using AOT;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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

        public delegate VoxelType CalcType(Vector3Int chunkSize, Vector3Int globalIndex);

        [MonoPInvokeCallback(typeof(CalcType))]
        public abstract VoxelType CalculateBlockType(Vector3Int chunkSize, Vector3Int globalIndex);
        public virtual void GenerateBlocksFor(TerrainChunk chunk)
        {
            /*NativeArray<TerrainBlock> blocks = new NativeArray<TerrainBlock>(chunk.Blocks.Length, Allocator.TempJob);

            // Create a job
            var job = new GenerateBlocksJob
            {
                ChunkSize = chunk.Size,
                ChunkIndex = chunk.Index,
                Blocks = blocks,
                CalculateBlockTypeFunction = BurstCompiler.CompileFunctionPointer<CalcType>(CalculateBlockType)// Pass reference to the terrain generator
            };

            // Schedule the job
            JobHandle handle = job.Schedule(chunk.Blocks.Length, 64);
            handle.Complete();
            blocks.CopyTo(chunk.Blocks);
            blocks.Dispose();*/
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
    
    internal unsafe struct GenerateBlocksJob : IJobParallelFor
    {
        [ReadOnly]
        public Vector3Int ChunkSize;

        [ReadOnly]
        public Vector3Int ChunkIndex;

        public NativeArray<TerrainBlock> Blocks;

        public FunctionPointer<TerrainGeneratorBase.CalcType> CalculateBlockTypeFunction;

        public void Execute(int i)
        {
            var index = MathUtils.To3D(i, ChunkSize.x, ChunkSize.y);
            var globalIndex = index + ChunkIndex * ChunkSize;

            TerrainBlock block = new()
            {
                index = index,
                globalIndex = globalIndex,
                type = CalculateBlockTypeFunction.Invoke(ChunkSize, globalIndex)
            };

            Blocks[i] = block;
        }
    }
}