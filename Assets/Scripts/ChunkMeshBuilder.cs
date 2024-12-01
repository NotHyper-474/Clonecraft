using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Collections;

using Debug = UnityEngine.Debug;
using Unity.Jobs.LowLevel.Unsafe;

namespace Minecraft
{
    public sealed class ChunkMeshBuilder : System.IDisposable
    {
        private readonly TerrainManager manager;
        private TerrainChunkMesherGreedyJob greedyJob;
        private JobHandle jobHandle;

        private float avgMeshingTime;
        private int avgMeshingCount;
        private float lastTime;

        public ChunkMeshBuilder(TerrainManager manager, TerrainConfig config)
        {
            this.manager = manager;
            greedyJob = new TerrainChunkMesherGreedyJob()
            {
                vertices = new NativeList<Vector3>(Allocator.Persistent),
                triangles = new NativeList<int>(Allocator.Persistent),
                normals = new NativeList<Vector3>(Allocator.Persistent),
                uvs = new NativeList<Vector3>(Allocator.Persistent),
                blockSize = config.blockSize,
            };
        }

        ~ChunkMeshBuilder()
        {
            Dispose();
        }
        
        /*private static void ConvertVoxels(TerrainChunk chunk, ref NativeArray<VoxelType> voxelData)
        {
            Debug.Assert(voxelData.IsCreated, "Voxel data array should be initialized.");

            for (int i = 0; i < voxelData.Length; i++)
            {
                voxelData[i] = chunk.blocks[i].type;
            };
        }*/

        public Mesh JobsBuildChunk(TerrainChunk chunk)
        {
			Stopwatch s1 = new Stopwatch();
			s1.Start();

            greedyJob.chunkSize = new int3(chunk.Size.x, chunk.Size.y, chunk.Size.z);
            greedyJob.voxels = new NativeArray<TerrainBlock>(chunk.blocks, Allocator.TempJob);//new NativeArray<TerrainBlock>(chunk.blocks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            //ConvertVoxels(chunk, ref greedyJob.voxels);

            jobHandle = greedyJob.Schedule();
            jobHandle.Complete();
            s1.Stop();
			// Debug.Log($"Elapsed meshing time for {chunk.index.ToString()}: {s1.Elapsed.TotalMilliseconds}");
            if (avgMeshingCount > 0 && lastTime <= Time.time)
            {
                Debug.Log("Average meshing time: " + avgMeshingTime/avgMeshingCount);
                lastTime = Time.time + 1f;
            }
            avgMeshingTime += (float)s1.Elapsed.TotalMilliseconds;
            avgMeshingCount++;
            greedyJob.voxels.Dispose();

            return BuildMesh();
        }

        private Mesh BuildMesh()
        {
            Mesh mesh = new Mesh
            {
                // Allows bigger meshes however takes more memory and bandwidth (also generally not necessary)
                //indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            };
            var managedTriangles = new int[greedyJob.triangles.Length];
            greedyJob.triangles.AsArray().CopyTo(managedTriangles);

            mesh.SetVertices(greedyJob.vertices.AsArray());
            mesh.SetTriangles(managedTriangles, 0, false);
            mesh.SetUVs(0, greedyJob.uvs.AsArray());
            mesh.SetNormals(greedyJob.normals.AsArray());
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }

        public void Clear()
        {
            // Clear all mesh data
            if (!greedyJob.vertices.IsCreated) return;
            greedyJob.vertices.Clear();
            greedyJob.triangles.Clear();
            greedyJob.normals.Clear();
            greedyJob.uvs.Clear();
        }

        public void Dispose()
        {
            // Avoid errors because of the arrays being used after disposed
            jobHandle.Complete();
            if (greedyJob.vertices.IsCreated) greedyJob.vertices.Dispose();
            if (greedyJob.triangles.IsCreated) greedyJob.triangles.Dispose();
            if (greedyJob.normals.IsCreated) greedyJob.normals.Dispose();
            if (greedyJob.uvs.IsCreated) greedyJob.uvs.Dispose();
            //if (greedyJob.neighborVoxels.IsCreated) greedyJob.neighborVoxels.Dispose();
            //System.GC.SuppressFinalize(this);
        }
    }
}