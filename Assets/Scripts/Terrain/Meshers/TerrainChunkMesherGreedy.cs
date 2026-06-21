using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static System.Diagnostics.Stopwatch;

namespace Minecraft
{
    [CreateAssetMenu(menuName = "Clonecraft/Meshers/Greedy", fileName = "Greedy Mesher")]
    public sealed class TerrainChunkMesherGreedy : TerrainChunkMesherBase
    {
        public struct TerrainGreedyJobData : ITerrainJobData
        {
            public JobHandle Handle { get; set; }
            public ITerrainMesherJob Job { get; set; }
            
            public TerrainChunk chunk;
            public Mesh chunkMesh;
            public Mesh.MeshDataArray meshArray;
        }
        
        public override ITerrainJobData GenerateMeshFor(TerrainChunk chunk, TerrainChunk[] neighbors)
        {
            var mesh = chunk.ChunkMesh;
            if (!mesh)
            {
                mesh = new Mesh();
            }
            
            var meshArray = Mesh.AllocateWritableMeshData(mesh);
            var job = new TerrainChunkMesherGreedyJob
            {
                mesh = meshArray[0],
                chunkSize = new int3(chunk.Size.x, chunk.Size.y, chunk.Size.z),
                blockSize = TerrainManager.Instance.TerrainConfig.blockSize,
                voxels = new NativeArray<TerrainBlock>(chunk.Blocks, Allocator.TempJob)
            };
            return new TerrainGreedyJobData()
            {
                Job = job,
                Handle = job.ScheduleByRef(),
                chunk = chunk,
                chunkMesh = mesh,
                meshArray = meshArray,
            };
        }

        public override void ApplyData(ITerrainJobData data)
        {
            if (data is not TerrainGreedyJobData jobData)
                throw new ArgumentException($"Incorrect type for {nameof(data)}");
            
            if (!jobData.chunkMesh)
                throw new NullReferenceException("Job chunkMesh is null");
            
            Mesh.ApplyAndDisposeWritableMeshData(jobData.meshArray, jobData.chunkMesh);
            jobData.Job.Dispose();
            
            jobData.chunk.SetMesh(jobData.chunkMesh, null);
            jobData.chunkMesh.RecalculateBounds();
        }
    }
    
    
}