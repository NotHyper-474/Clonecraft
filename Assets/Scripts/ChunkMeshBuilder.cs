using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Collections;

namespace Minecraft
{
    public sealed class ChunkMeshBuilder : System.IDisposable
    {
        private readonly TerrainManager manager;
        private TerrainChunkMeshBuildJob greedyJob;

        private readonly static Vector3Int[] voxelSideChecks =
        {
            new Vector3Int(-1, 0, 0),	// Left
			new Vector3Int(1, 0, 0),	// Right
			new Vector3Int(0, 1, 0),	// Top
			new Vector3Int(0, -1, 0),	// Bottom
			new Vector3Int(0, 0, 1),	// Forward
			new Vector3Int(0, 0, -1),	// Backward
		};

        private readonly static Dictionary<Vector3Int, VoxelSides> posToSide = new Dictionary<Vector3Int, VoxelSides> {
            { voxelSideChecks[0], VoxelSides.WEST },
            { voxelSideChecks[1], VoxelSides.EAST },
            { voxelSideChecks[2], VoxelSides.TOP },
            { voxelSideChecks[3], VoxelSides.BOTTOM },
            { voxelSideChecks[4], VoxelSides.NORTH },
            { voxelSideChecks[5], VoxelSides.SOUTH },
        };

        public ChunkMeshBuilder(TerrainManager manager)
        {
            this.manager = manager;
            greedyJob = new TerrainChunkMeshBuildJob()
            {
                vertices = new NativeList<Vector3>(Allocator.Persistent),
                triangles = new NativeList<int>(Allocator.Persistent),
                normals = new NativeList<Vector3>(Allocator.Persistent),
                uvs = new NativeList<Vector3>(Allocator.Persistent),
                blockSize = 1f,
            };
        }

        ~ChunkMeshBuilder()
        {
            Dispose();
        }

#if UNITY_EDITOR
        // TODO: Remove or move these
        [UnityEditor.MenuItem("Jobs/LeakDetection/Off")]
        public static void OffLeak()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
        }

        [UnityEditor.MenuItem("Jobs/LeakDetection/On")]
        public static void OnLeak()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.Enabled;
        }

        [UnityEditor.MenuItem("Jobs/LeakDetection/Full Detection (Expensive)")]
        public static void FullLeak()
        {
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
        }
#endif

        private void CullVoxels(TerrainChunk chunk, out MaskBlock[] result)
        {
            var blocks = new MaskBlock[chunk.Size.x * chunk.Size.y * chunk.Size.z];
            for (int i = 0; i < chunk.blocks.Length; i++)
            {
                //var idx = MMMath.To3D(i, chunk.Size.x, chunk.Size.y);
                if (chunk.blocks[i].IsEmpty()) continue;
                blocks[i].type = (int)chunk.blocks[i].type;
				blocks[i].shownSides = VoxelSides.ALL;
                /*for (int j = 0; j < voxelSideChecks.Length; j++)
                {
                    if (manager.GetBlockAt(chunk.blocks[i].globalIndex + voxelSideChecks[j]).IsEmpty())
                    {
                        var side = posToSide[voxelSideChecks[j]];
                        blocks[i].shownSides |= side;
                    }
                }*/
            }
            result = blocks;
        }

        public Mesh JobsBuildChunk(TerrainChunk chunk, params Vector2[] uvTest)
        {
			Stopwatch s1 = new Stopwatch();
			s1.Start();
            CullVoxels(chunk, out MaskBlock[] blocks);
			
            greedyJob.chunkSize = new int3(chunk.Size.x, chunk.Size.y, chunk.Size.z);
            greedyJob.blocks = new NativeArray<MaskBlock>(blocks, Allocator.TempJob);

            JobHandle handle = greedyJob.Schedule();
            handle.Complete();
            s1.Stop();
			UnityEngine.Debug.Log("Elapsed meshing time: " + s1.Elapsed.TotalMilliseconds);
            greedyJob.blocks.Dispose();

            return BuildMesh();
        }

        private Mesh BuildMesh()
        {
            Mesh mesh = new Mesh
            {
                // Allows bigger meshes however takes more memory and bandwidth
                //indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = greedyJob.vertices.ToArray(),
                triangles = greedyJob.triangles.ToArray(),
                normals = greedyJob.normals.ToArray()
            };
            mesh.SetUVs(0, greedyJob.uvs.ToArray());
            mesh.RecalculateTangents();
            //mesh.Optimize();

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
            greedyJob.vertices.Dispose();
            greedyJob.triangles.Dispose();
            greedyJob.normals.Dispose();
            greedyJob.uvs.Dispose();
            System.GC.SuppressFinalize(this);
        }
    }
}