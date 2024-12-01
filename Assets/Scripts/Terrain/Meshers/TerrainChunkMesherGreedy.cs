using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Minecraft
{
    [CreateAssetMenu(menuName = "Clonecraft/Meshers/Greedy", fileName = "Greedy Mesher")]
    public sealed class TerrainChunkMesherGreedy : TerrainChunkMesherBase
    {
        private TerrainChunkMesherGreedyJob _job = new()
        {
            vertices = new NativeList<Vector3>(Allocator.Persistent),
            triangles = new NativeList<int>(Allocator.Persistent),
            uvs = new NativeList<Vector3>(Allocator.Persistent),
            normals = new NativeList<Vector3>(Allocator.Persistent),
        };
        private bool _jobInitialized;

        private void OnDisable()
        {
            Dispose();
        }

        public override void GenerateMeshFor(TerrainChunk chunk, TerrainChunk[] neighbors = null)
        {
            if (neighbors != null && neighbors.Length != 0)
            {
                Debug.LogWarning("Neighbour chunks are going to be ignored.");
            }

            ClearJob();
            _job.chunkSize = new int3(chunk.Size.x, chunk.Size.y, chunk.Size.z);
            _job.blockSize = TerrainManager.Instance.TerrainConfig.blockSize;
            _job.voxels = new NativeArray<TerrainBlock>(chunk.blocks, Allocator.TempJob);
            _job.RunByRef();

            var mesh = chunk.ChunkMesh;
            if (!mesh)
                mesh = new Mesh();
            
            mesh.Clear();
            mesh.SetVertices(_job.vertices.AsArray());
            mesh.SetIndices(_job.triangles.AsArray(), MeshTopology.Triangles, 0);
            mesh.SetNormals(_job.normals.AsArray());
            mesh.SetUVs(0, _job.uvs.AsArray());
            chunk.SetMesh(mesh, null);

            _job.voxels.Dispose();
        }

        private void ClearJob()
        {
            _job.vertices.Clear();
            _job.triangles.Clear();
            _job.uvs.Clear();
            _job.normals.Clear();
        } 

        private void Dispose()
        {
            if (_job.vertices.IsCreated) _job.vertices.Dispose();
            if (_job.triangles.IsCreated) _job.triangles.Dispose();
            if (_job.normals.IsCreated) _job.normals.Dispose();
            if (_job.uvs.IsCreated) _job.uvs.Dispose();
        }
    }
}