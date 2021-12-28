using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace Minecraft
{
    public sealed class ChunkMeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector3> uvs = new List<Vector3>(); //TODO: UV
        private readonly TerrainManager manager;

        public ChunkMeshBuilder(TerrainManager manager)
        {
            this.manager = manager;
        }

        public Mesh BuildBlockMesh(Vector3 position)
        {
            AddBlockFace_Up(position - Vector3.one * 0.5f);
            AddBlockFace_Down(position - Vector3.one * 0.5f);
            AddBlockFace_Left(position - Vector3.one * 0.5f);
            AddBlockFace_Right(position - Vector3.one * 0.5f);
            AddBlockFace_Forward(position - Vector3.one * 0.5f);
            AddBlockFace_Back(position - Vector3.one * 0.5f);

            return BuildMesh();
        }

        public Mesh JobsBuildChunk(TerrainChunk chunk, params Vector2[] uvTest)
        {
            TerrainChunkMeshBuildJob job = new TerrainChunkMeshBuildJob()
            {
                vertices = new NativeList<Vector3>(Allocator.TempJob),
                triangles = new NativeList<int>(Allocator.TempJob),
                uvs = new NativeList<Vector3>(Allocator.TempJob),

                blocks = new NativeArray<TerrainBlock>(chunk.blocks, Allocator.TempJob),
                maskedBlockTypes = new NativeArray<float>(chunk.Size.x * chunk.Size.y, Allocator.TempJob),
                chunkSize = new int3(chunk.Size.x, chunk.Size.y, chunk.Size.z)
            };
            /*job.topLeft = uvTest[0];
			job.topRight = uvTest[1];
			job.bottomLeft = uvTest[2];
			job.bottomRight = uvTest[3];*/

            JobHandle handle = job.Schedule();
            handle.Complete();

            for (int i = 0; i < job.vertices.Length; i++)
            {
                vertices.Add(job.vertices[i]);
            }
            for (int i = 0; i < job.triangles.Length; i++)
            {
                triangles.Add(job.triangles[i]);
            }
            for (int i = 0; i < job.uvs.Length; i++)
            {
                uvs.Add(job.uvs[i]);
            }
            //manager.defaultMaterial.SetFloatArray("_BlockData", job.maskedBlockTypes.ToArray());

            job.vertices.Dispose();
            job.triangles.Dispose();
            job.uvs.Dispose();
            job.blocks.Dispose();
            job.maskedBlockTypes.Dispose();

            return BuildMesh();
        }

        public Mesh BuildChunk(TerrainChunk chunk)
        {
            // Mostly based on a Greedy Meshing algorithm by Mikola Lysenko and code based on Rob O'Leary
            const int SOUTH = 0, NORTH = 1,
                EAST = 2, WEST = 3, TOP = 4, BOTTOM = 5;
            var emptyBlock = new TerrainBlock(BlockType.Air, Vector3Int.zero, Vector3Int.zero, 0);

            // Sweep over each axis (X, Y and Z)
            for (bool backFace = true, b = false; b != backFace; backFace = backFace && b, b = !b)
            {
                for (var d = 0; d < 3; ++d)
                {
                    int i, j, k, l, w, h;
                    int u = (d + 1) % 3;
                    int v = (d + 2) % 3;
                    var x = new Vector3Int();
                    var q = new Vector3Int();
                    int side = 0;


                    if (d == 0)
                        side = backFace ? WEST : EAST;
                    if (d == 1)
                        side = backFace ? BOTTOM : TOP;
                    if (d == 2)
                        side = backFace ? SOUTH : NORTH;

                    var mask = new TerrainBlock[chunk.Size[u] * chunk.Size[v]];
                    q[d] = 1;

                    // Check each slice of the chunk one at a time
                    for (x[d] = -1; x[d] < chunk.Size[d];)
                    {
                        // Compute the mask
                        var n = 0;
                        for (x[v] = 0; x[v] < chunk.Size[v]; ++x[v])
                        {
                            for (x[u] = 0; x[u] < chunk.Size[u]; ++x[u])
                            {
                                // q determines the direction (X, Y or Z) that we are searching
                                // m.IsBlockAt(x,y,z) takes chunk positions and returns true if a block exists there


                                var blockCurrent = (x[d] >= 0) ? chunk.GetBlock(x) : emptyBlock;
                                var blockCompare = (x[d] < chunk.Size[d] - 1) ? chunk.GetBlock(x + q) : emptyBlock;
                                //manager.GetBlockTypeAt(chunk, (chunk.index * chunk.Size) + x) : emptyBlock;
                                //manager.GetBlockTypeAt(chunk, (chunk.index * chunk.Size) + x + q) : emptyBlock;

                                blockCurrent.metadata = (byte)side;
                                blockCompare.metadata = (byte)side;

                                mask[n++] = (!blockCurrent.IsEmpty() && !blockCompare.IsEmpty() && blockCurrent.Equals(blockCompare))
                                    ? emptyBlock : backFace ? blockCompare : blockCurrent;
                            }
                        }

                        ++x[d];

                        n = 0;

                        // Generate a mesh from the mask using lexicographic ordering,      
                        //   by looping over each block in this slice of the chunk
                        for (j = 0; j < chunk.Size[v]; j++)
                        {
                            for (i = 0; i < chunk.Size[u];)
                            {
                                if (!mask[n].IsEmpty())
                                {
                                    // Compute the width of this quad and store it in w                        
                                    //   This is done by searching along the current axis until mask[n + w] is false
                                    for (w = 1; i + w < chunk.Size[u] && !mask[n + w].IsEmpty(); w++) { }

                                    // Compute the height of this quad and store it in h                        
                                    //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                                    //   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                                    //   greedy meshing will attempt to expand this quad out to CHUNK_SIZE x 5, but will stop if it reaches a hole in the mask

                                    var done = false;
                                    for (h = 1; j + h < chunk.Size[v]; h++)
                                    {
                                        // Check each block next to this quad
                                        for (k = 0; k < w; ++k)
                                        {
                                            // If there's a hole in the mask, exit
                                            if (mask[n + k + h * chunk.Size[u]].IsEmpty())
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
                                    var du = new Vector3Int();
                                    du[u] = w;

                                    var dv = new Vector3Int();
                                    dv[v] = h;

                                    var blockMinVertex = Vector3.one * -0.5f;

                                    CreateQuad(
                                        blockMinVertex + x,
                                        blockMinVertex + x + du,
                                        blockMinVertex + x + du + dv,
                                        blockMinVertex + x + dv,
                                        w,
                                        h,
                                        backFace
                                        );

                                    // Clear this part of the mask, so we don't add duplicate faces
                                    for (l = 0; l < h; ++l)
                                        for (k = 0; k < w; ++k)
                                            mask[n + k + l * chunk.Size[u]] = emptyBlock;

                                    // Increment counters and continue
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
            return BuildMesh();
        }

        private Mesh BuildMesh()
        {
            Mesh mesh = new Mesh
            {
                // Allows bigger meshes however takes more memory and bandwidth
                //indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
            };
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }

        public void CreateQuad(
            Vector3 bottomLeft,
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            int width,
            int height,
            bool backFace
            )
        {
            triangles.AddRange(backFace ?
                new int[] {
            vertices.Count + 1,
            vertices.Count + 2,
            vertices.Count,

            vertices.Count + 1,
            vertices.Count + 3,
            vertices.Count + 2,
                }
                : new int[] {
                vertices.Count,
                vertices.Count + 2,
                vertices.Count + 1,

                vertices.Count + 2,
                vertices.Count + 3,
                vertices.Count + 1,
                    });

            vertices.AddRange(new Vector3[] {
                bottomLeft,
                bottomRight,
                topLeft,
                topRight
                });

            //var minVert = Vector2.one * 0.5f;
            //uvs.AddRange(GetUVCoordinates(0, 0));
        }

        public void Clear()
        {
            // Clear all mesh data
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
        }

        private void AddBlockFace_Up(Vector3 blockMinVertex)
        {
            AddFacesForNextFourVertices();

            vertices.Add(blockMinVertex + Vector3.up);
            vertices.Add(blockMinVertex + new Vector3(0, 1, 1));
            vertices.Add(blockMinVertex + new Vector3(1, 1, 1));
            vertices.Add(blockMinVertex + new Vector3(1, 1, 0));
        }

        private void AddBlockFace_Down(Vector3 blockMinVertex)
        {
            AddFacesForNextFourVertices();

            vertices.Add(blockMinVertex + new Vector3(0, 0, 0));
            vertices.Add(blockMinVertex + new Vector3(1, 0, 0));
            vertices.Add(blockMinVertex + new Vector3(1, 0, 1));
            vertices.Add(blockMinVertex + new Vector3(0, 0, 1));
        }

        private void AddBlockFace_Forward(Vector3 blockMinVertex)
        {
            AddFacesForNextFourVertices();

            vertices.Add(blockMinVertex + new Vector3(1, 0, 1));
            vertices.Add(blockMinVertex + new Vector3(1, 1, 1));
            vertices.Add(blockMinVertex + new Vector3(0, 1, 1));
            vertices.Add(blockMinVertex + new Vector3(0, 0, 1));
        }

        private void AddBlockFace_Back(Vector3 blockMinVertex)
        {
            AddFacesForNextFourVertices();

            vertices.Add(blockMinVertex + new Vector3(0, 0, 0));
            vertices.Add(blockMinVertex + new Vector3(0, 1, 0));
            vertices.Add(blockMinVertex + new Vector3(1, 1, 0));
            vertices.Add(blockMinVertex + new Vector3(1, 0, 0));
        }

        private void AddBlockFace_Left(Vector3 blockMinVertex)
        {
            AddFacesForNextFourVertices();

            vertices.Add(blockMinVertex + Vector3.forward);
            vertices.Add(blockMinVertex + new Vector3(0, 1, 1));
            vertices.Add(blockMinVertex + Vector3.up);
            vertices.Add(blockMinVertex + Vector3.zero);
        }

        private void AddBlockFace_Right(Vector3 blockMinVertex)
        {
            AddFacesForNextFourVertices();

            vertices.Add(blockMinVertex + Vector3.right);
            vertices.Add(blockMinVertex + new Vector3(1, 1, 0));
            vertices.Add(blockMinVertex + new Vector3(1, 1, 1));
            vertices.Add(blockMinVertex + new Vector3(1, 0, 1));
        }

        private void AddFacesForNextFourVertices()
        {
            triangles.Add(vertices.Count);
            triangles.Add(vertices.Count + 1);
            triangles.Add(vertices.Count + 2);
            triangles.Add(vertices.Count);
            triangles.Add(vertices.Count + 2);
            triangles.Add(vertices.Count + 3);
        }
    }
}