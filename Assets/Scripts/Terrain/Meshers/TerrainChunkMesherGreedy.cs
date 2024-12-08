using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine;

namespace Minecraft
{
    [CreateAssetMenu(menuName = "Clonecraft/Meshers/Greedy", fileName = "Greedy Mesher")]
    public sealed class TerrainChunkMesherGreedy : TerrainChunkMesherBase
    {
        private TerrainChunkMesherGreedyJob _job;

        public override void GenerateMeshFor(TerrainChunk chunk, TerrainChunk[] neighbors = null)
        {
            var mesh = chunk.ChunkMesh;
            if (!mesh)
            {
                mesh = new Mesh();
            }

            var meshArray = Mesh.AllocateWritableMeshData(mesh);
            _job.mesh = meshArray[0];
            _job.chunkSize = new int3(chunk.Size.x, chunk.Size.y, chunk.Size.z);
            _job.blockSize = TerrainManager.Instance.TerrainConfig.blockSize;
            _job.voxels = new NativeArray<TerrainBlock>(chunk.Blocks, Allocator.TempJob);
            _job.Schedule().Complete();
            Mesh.ApplyAndDisposeWritableMeshData(meshArray, mesh);
            
            // FIXME: For some reason setting bounds directly doesn't work so this is needed as a workaround, investigate
            mesh.RecalculateBounds();
            chunk.SetMesh(mesh, null);

            _job.voxels.Dispose();
        }
    }
}