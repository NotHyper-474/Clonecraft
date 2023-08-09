using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using System.Runtime.CompilerServices;

namespace Minecraft
{
    [BurstCompile, BurstCompatible]
    public struct TerrainChunkMeshBuildJob : IJob
    {
        public NativeList<Vector3> vertices;
        [WriteOnly] public NativeList<int> triangles;
        [WriteOnly] public NativeList<Vector3> normals;
        [WriteOnly] public NativeList<Vector3> uvs;

        [ReadOnly] public NativeArray<VoxelType> voxels;
        [ReadOnly] public NativeBitArray neighborVoxels;
        [ReadOnly] public int3 chunkSize;
        [ReadOnly] public float blockSize;

        private const int SOUTH = 0, NORTH = 1,
                EAST = 2, WEST = 3, TOP = 4, BOTTOM = 5;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal VoxelType GetVoxel(int3 pos)
        {
            // TODO: Check for blocks in neighbor chunk

            return voxels[MathUtils.To1D(pos.x, pos.y, pos.z, chunkSize.x, chunkSize.y)];
        }

        /// <summary>
        /// * Mostly based on a Greedy Meshing algorithm by Mikola Lysenko and code based on Cleo McCoy (formely Rob O'Leary)
        /// </summary>
        public void Execute()
        {
            // Cycle through all 3 axis
            for (int d = 0; d < 3; d++)
            {
                bool backFace = false;
                int u = (d + 1) % 3; // Which axis of the slice to cycle through
                int v = (d + 2) % 3;
                var x = new int3();
                var q = new int3();
                int side = 0;
                // Ulong first bytes (0xFFFFFFFF) represent normal and the rest represent its type
                var mask = new NativeArray<ulong>(chunkSize[u] * chunkSize[v], Allocator.Temp, NativeArrayOptions.UninitializedMemory);
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
                            int min = 0;//q.y != 0 ? 0 : -1;
                            int max = chunkSize[d]-1;//q.y != 0 ? chunkSize[d] - 1 : chunkSize[d]; 
                            var blockCurrent = (x[d] >= min) ? GetVoxel(x) : default;
                            var blockCompare = (x[d] < max) ? GetVoxel(x + q) : default;

                            bool bCurrentOpaque = blockCurrent != default;
                            bool bCompareOpaque = blockCompare != default;

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
                                mask[n] |= (0 & 0x3UL) << 32;
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
                                //   This is done by searching along the current axis until mask[n + w] is false
                                int w = 1;
                                int h = 1;
                                for (; i + w < chunkSize[u] && mask[n + w].Equals(mask[n]); w++) { }

                                // Compute the height of this quad and store it in h                        
                                //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                                //   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                                //   greedy meshing will attempt to expand this quad out to CHUNK_SIZE x 5, but will stop if it reaches a hole in the mask

                                var done = false;
                                for (; j + h < chunkSize[v]; h++)
                                {
                                    // Check each block next to this quad
                                    for (int k = 0; k < w; ++k)
                                    {
                                        // If there's a hole in the mask or it is not equal to the next face, exit
                                        if (mask[n + k + h * chunkSize[u]] == emptyBlock || !mask[n + k + h * chunkSize[u]].Equals(mask[n]))
                                        {
                                            done = true;
                                            break;
                                        }
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

                                var blockMinVertex = new float3(-blockSize * 0.5f, -blockSize * 0.5f, -blockSize * 0.5f);

                                switch (d)
                                {
                                    case 0:
                                        side = backFace ? WEST : EAST;
                                        break;
                                    case 1:
                                        side = q.y * (int)(mask[n] >> 32 & 0x3UL) - 1 < 0 ? BOTTOM : TOP;
                                        break;
                                    case 2:
                                        side = backFace ? SOUTH : NORTH;
                                        break;
                                }

                                CreateQuad(
                                    blockMinVertex + x,
                                    blockMinVertex + x + du,
                                    blockMinVertex + x + du + dv,
                                    blockMinVertex + x + dv,
                                    w,
                                    h,
                                    new Vector3(q.x, q.y, q.z),
                                    mask[n],
                                    side
                                    );

                                // Clear this part of the mask, so we don't add duplicate faces
                                for (int l = 0; l < h; ++l)
                                    for (int k = 0; k < w; ++k)
                                    {
                                        mask[n + k + l * chunkSize[u]] = emptyBlock;
                                    }

                                // Increment counters by width and width? of the quad and continue
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
        }

        public void CreateQuad(
                float3 bottomLeft,
                float3 topLeft,
                float3 topRight,
                float3 bottomRight,
                float width,
                float height,
                Vector3 axisMask,
                ulong mask,
                int side
                )
        {
            int maskNormal = (int)(mask >> 32 & 0x3UL) - 1;
            triangles.Add(vertices.Length);
            triangles.Add(vertices.Length + 2 + maskNormal);
            triangles.Add(vertices.Length + 2 - maskNormal);
            triangles.Add(vertices.Length + 3);
            triangles.Add(vertices.Length + 1 - maskNormal);
            triangles.Add(vertices.Length + 1 + maskNormal);


            vertices.Add(bottomLeft * blockSize);
            vertices.Add(bottomRight * blockSize);
            vertices.Add(topLeft * blockSize);
            vertices.Add(topRight * blockSize);
            Vector3 normal = axisMask * maskNormal;

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            AddUVForSide(height, width, side, normal, (VoxelType)(mask & 0xFFFFFFFFUL));
        }

        public void AddUVForSide(float width, float height, int side, Vector3 normal, VoxelType block)
        {
            int w = 0;
            if (block == VoxelType.Air)
            {
                w = 13;
            }
            if (block == VoxelType.Grass)
            {
                switch (side)
                {
                    case NORTH:
                    case SOUTH:
                    case WEST:
                    case EAST:
                        w = 8;
                        break;
                    case TOP:
                        w = 13;
                        break;
                    case BOTTOM:
                        w = 12;
                        break;
                    default:
                        w = 12;
                        break;
                }
            }
            else if (block == VoxelType.Stone)
            {
                w = 15;
            }
            else if (block == VoxelType.Dirt)
            {
                w = 12;
            }
            if (block == VoxelType.OakLog) {
                if (side == TOP || side == BOTTOM) w = 10;
                else w = 14;
            }

            if (normal.x == -1 || normal.x == 1)
            {
                uvs.Add(new Vector3(0, 0, w));
                uvs.Add(new Vector3(width, 0, w));
                uvs.Add(new Vector3(0, height, w));
                uvs.Add(new Vector3(width, height, w));
            }
            else
            {
                uvs.Add(new Vector3(0, 0, w));
                uvs.Add(new Vector3(0, width, w));
                uvs.Add(new Vector3(height, 0, w));
                uvs.Add(new Vector3(height, width, w));
            }
        }
    }
}