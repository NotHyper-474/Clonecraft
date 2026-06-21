using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;

namespace Minecraft
{
    internal struct MeshData
    {
        // TODO: Could definitely benefit from vertex packing
        public NativeList<float3> vertexData;
        public NativeList<ushort> indexData;
        public NativeList<float3> normalData;
        public NativeList<float3> uvData;

        public void Initialize(Allocator allocator = Allocator.TempJob)
        {
            // Use provided allocator (default to TempJob) when used inside a job
            vertexData = new NativeList<float3>(0, allocator);
            indexData = new NativeList<ushort>(0, allocator);
            normalData = new NativeList<float3>(0, allocator);
            uvData = new NativeList<float3>(0, allocator);
        }

        public void Dispose()
        {
            vertexData.Dispose();
            indexData.Dispose();
            normalData.Dispose();
            uvData.Dispose();
        }
    }
    
    [BurstCompile]
    public struct TerrainChunkMesherGreedyJob : ITerrainMesherJob
    {
        public Mesh.MeshData mesh;

        [ReadOnly] public NativeArray<TerrainBlock> voxels;
        [ReadOnly] public int3 chunkSize;
        [ReadOnly] public float blockSize;

        private int _vertexCount;
        private int _indexCount;

        // TODO: Would an enum work as well?
        private const int SOUTH = 0,
            NORTH = 1,
            EAST = 2,
            WEST = 3,
            TOP = 4,
            BOTTOM = 5;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private VoxelType GetVoxel(int3 pos)
        {
            // TODO: Check for blocks in neighbor chunk

            return voxels[MathUtils.To1D(pos.x, pos.y, pos.z, chunkSize.x, chunkSize.y)].type;
        }

        /// <summary>
        /// * Mostly based on a Greedy Meshing algorithm by Mikola Lysenko and code based on Cleo McCoy (formerly Rob O'Leary)
        /// </summary>
        public void Execute()
        {
            _vertexCount = 0;
            _indexCount = 0;

            var meshData = new MeshData();
            meshData.Initialize();

            // Cycle through all 3 axes
            for (int d = 0; d < 3; d++)
            {
                bool backFace = false;
                int u = (d + 1) % 3; // Which axis of the slice to cycle through, "horizontally"
                int v = (d + 2) % 3; // Which axis of the slice to cycle through, "vertically"
                var x = new int3();
                var q = new int3();
                int side = 0;
                // Ulong first bytes (0xFFFFFFFF) represent normal and the rest represent its type
                var mask = new NativeArray<ulong>(chunkSize[u] * chunkSize[v], Allocator.Temp,
                    NativeArrayOptions.UninitializedMemory);
                const ulong emptyBlock = (1 & 0x3UL) << 32;

                q[d] = 1;

                // Check each slice of the chunk one at a time
                for (x[d] = -1; x[d] < chunkSize[d];)
                {
                    // Compute the mask
                    var n = 0;
                    for (x[v] = 0; x[v] < chunkSize[v]; ++x[v])
                    {
                        for (x[u] = 0; x[u] < chunkSize[u]; ++x[u])
                        {
                            // q determines the direction (X, Y or Z) that we are searching
                            int min = 0; //q.y != 0 ? 0 : -1;
                            int max = chunkSize[d] - 1; //q.y != 0 ? chunkSize[d] - 1 : chunkSize[d]; 
                            var blockCurrent = (x[d] >= min) ? GetVoxel(x) : VoxelType.Air;
                            var blockCompare = (x[d] < max) ? GetVoxel(x + q) : VoxelType.Air;

                            bool bCurrentOpaque = blockCurrent != VoxelType.Air;
                            bool bCompareOpaque = blockCompare != VoxelType.Air;

                            if (bCurrentOpaque == bCompareOpaque)
                                mask[n++] = emptyBlock;
                            else if (bCurrentOpaque)
                            {
                                // Set current block normal to 1 (represented by 2)
                                mask[n] = 0;
                                mask[n] |= (2 & 0x3UL) << 32;
                                mask[n++] |= (ulong)blockCurrent & 0xFFFFFFFFUL;
                                backFace = false;
                            }
                            else
                            {
                                // Set compare block normal to -1 (represented by 0)
                                mask[n] = 0;
                                mask[n] |= 0; //(0 & 0x3UL) << 32;
                                mask[n++] |= (ulong)blockCompare & 0xFFFFFFFFUL;
                                backFace = true;
                            }
                        }
                    }

                    x[d]++;

                    n = 0;

                    // Generate a mesh from the mask using lexicographic ordering,
                    // by looping over each face in this slice of the chunk
                    for (int j = 0; j < chunkSize[v]; j++)
                    {
                        for (int i = 0; i < chunkSize[u];)
                        {
                            if (mask[n] != emptyBlock)
                            {
                                // Compute the width of this quad and store it in w
                                // This is done by searching along the current axis until mask[n + w] is false
                                int w = 1;
                                int h = 1;
                                for (; i + w < chunkSize[u] && mask[n + w].Equals(mask[n]); w++)
                                {
                                }

                                //   Compute the height of this quad and store it in h
                                //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                                //   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                                //   greedy meshing will attempt to expand this quad out to CHUNK_SIZE x 5, but will stop if it reaches a hole (or different block) in the mask

                                var done = false;
                                for (; j + h < chunkSize[v]; h++)
                                {
                                    // Check each block next to this quad
                                    for (int k = 0; k < w; ++k)
                                    {
                                        // If there's a hole in the mask, or it is not equal to the next face, exit
                                        if (mask[n + k + h * chunkSize[u]].Equals(mask[n])) continue;
                                        done = true;
                                        break;
                                    }

                                    if (done)
                                        break;
                                }

                                x[u] = i;
                                x[v] = j;


                                // du and dv determine the size and orientation of this face
                                var du = new int3();
                                du[u] = w;

                                var dv = new int3();
                                dv[v] = h;

                                var blockMinVertex = new float3(-0.5f, -0.5f,
                                    -0.5f);

                                side = d switch
                                {
                                    0 => backFace ? WEST : EAST,
                                    1 => q.y * (int)(mask[n] >> 32 & 0x3UL) - 1 < 0 ? BOTTOM : TOP,
                                    2 => backFace ? SOUTH : NORTH,
                                    _ => side
                                };

                                float3 bottomLeft = blockSize * (float3)x;
                                float3 topLeft = blockSize * (float3)(x + du);
                                float3 topRight = blockSize * (float3)(x + du + dv);
                                float3 bottomRight = blockSize * (float3)(x + dv);

                                CreateQuad(
                                    blockMinVertex + bottomLeft,
                                    blockMinVertex + topLeft,
                                    blockMinVertex + topRight,
                                    blockMinVertex + bottomRight,
                                    w,
                                    h,
                                    new Vector3(q.x, q.y, q.z),
                                    mask[n],
                                    side,
                                    ref meshData
                                );

                                // Clear this part of the mask, so we don't add duplicate faces
                                for (int l = 0; l < h; ++l)
                                {
                                    for (int k = 0; k < w; ++k)
                                    {
                                        mask[n + k + l * chunkSize[u]] = emptyBlock;
                                    }
                                }

                                // Increment counters by width of the quad and continue
                                i += w;
                                n += w;
                            }
                            else
                            {
                                i++;
                                n++;
                            }
                        }
                    }
                }
            }

            NativeArray<VertexAttributeDescriptor> attributes = new(3, Allocator.Temp, 
                NativeArrayOptions.UninitializedMemory);
            attributes[0] = new VertexAttributeDescriptor(VertexAttribute.Position);
            attributes[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, stream: 1);
            attributes[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 3, stream: 2);
            
            mesh.SetVertexBufferParams(_vertexCount, attributes);
            mesh.SetIndexBufferParams(_indexCount, IndexFormat.UInt16);
            mesh.subMeshCount = 1;
            
            mesh.GetVertexData<float3>().CopyFrom(meshData.vertexData.AsArray());
            mesh.GetVertexData<float3>(1).CopyFrom(meshData.normalData.AsArray());
            mesh.GetVertexData<float3>(2).CopyFrom(meshData.uvData.AsArray());
            mesh.GetIndexData<ushort>().CopyFrom(meshData.indexData.AsArray());

            var f32ChunkSize = math.float3(chunkSize);
            var offset = new float3(1f, 1f, 1f) * 0.5f;
            var descriptor = new SubMeshDescriptor(0, _indexCount)
            {
                bounds = new Bounds(0.5f * blockSize * f32ChunkSize - offset, f32ChunkSize * blockSize),
            };
            //Debug.Log($"{descriptor.bounds.center.x}, {descriptor.bounds.center.y}, {descriptor.bounds.center.z}");

            mesh.SetSubMesh(0, descriptor, MeshUpdateFlags.DontRecalculateBounds);
            attributes.Dispose();
            meshData.Dispose();
        }

        public void Dispose()
        {
            voxels.Dispose();
        }

        private void CreateQuad(
            float3 bottomLeft,
            float3 topLeft,
            float3 topRight,
            float3 bottomRight,
            float width,
            float height,
            float3 axisMask,
            ulong mask,
            int side,
            ref MeshData data
        )
        {
            int maskNormal = (int)(mask >> 32 & 0x3UL) - 1;
            int baseIndex = _vertexCount;

            // Add vertices
            data.vertexData.Add(bottomLeft);
            data.vertexData.Add(bottomRight);
            data.vertexData.Add(topLeft);
            data.vertexData.Add(topRight);

            // Add normals
            float3 normal = axisMask * maskNormal;
            data.normalData.Add(normal);
            data.normalData.Add(normal);
            data.normalData.Add(normal);
            data.normalData.Add(normal);

            // Add UVs
            AddUVForSide(height, width, side, normal, (VoxelType)(mask & 0xFFFFFFFFUL), ref data);

            // Add indices
            data.indexData.Add((ushort)(baseIndex + 0));
            data.indexData.Add((ushort)(baseIndex + 2 + maskNormal));
            data.indexData.Add((ushort)(baseIndex + 2 - maskNormal));
            data.indexData.Add((ushort)(baseIndex + 3));
            data.indexData.Add((ushort)(baseIndex + 1 - maskNormal));
            data.indexData.Add((ushort)(baseIndex + 1 + maskNormal));

            _vertexCount += 4;
            _indexCount += 6;
        }

        private void AddUVForSide(float width, float height, int side, float3 normal, VoxelType block,
            ref MeshData data)
        {
            // TODO: Softcode this
            int w = 0;
            if (block == VoxelType.Air)
            {
                w = 13;
            }

            if (block == VoxelType.Grass)
            {
                w = side switch
                {
                    NORTH or SOUTH or WEST or EAST => 8,
                    TOP => 13,
                    _ => 12
                };
            }
            else if (block == VoxelType.Stone)
            {
                w = 15;
            }
            else if (block == VoxelType.Dirt)
            {
                w = 12;
            }

            if (block == VoxelType.OakLog)
            {
                w = side is TOP or BOTTOM ? 10 : 14;
            }

            if (normal.x != 0)
            {
                data.uvData.Add(new float3(0, 0, w));
                data.uvData.Add(new float3(width, 0, w));
                data.uvData.Add(new float3(0, height, w));
                data.uvData.Add(new float3(width, height, w));
            }
            else
            {
                data.uvData.Add(new float3(0, 0, w));
                data.uvData.Add(new float3(0, width, w));
                data.uvData.Add(new float3(height, 0, w));
                data.uvData.Add(new float3(height, width, w));
            }
        }
    }
}