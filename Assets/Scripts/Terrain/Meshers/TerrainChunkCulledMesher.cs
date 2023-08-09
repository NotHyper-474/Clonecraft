using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public class TerrainChunkCulledMesher : TerrainChunkMesherBase
	{
		private readonly List<Vector3> vertices = new List<Vector3>();
		private readonly List<int> triangles = new List<int>();

		private readonly Vector3Int[] sideLookup = {
			Vector3Int.left, // West
			Vector3Int.right , // East
			Vector3Int.up , // Top
			Vector3Int.down, // Bottom
			Vector3Int.forward, // North
			Vector3Int.back, // South
		};

		private static Dictionary<Vector3Int, Vector3[]> _faceSideCache = new Dictionary<Vector3Int, Vector3[]>();

		public override void GenerateMeshFor(TerrainChunk chunk)
		{
			for (int j = 0; j < sideLookup.Length; j++)
			{
				for (int i = 0; i < chunk.blocks.Length; i++)
				{
					if (!chunk.blocks[i].IsEmpty() && chunk.GetBlock(chunk.blocks[i].index + sideLookup[j]).IsEmpty())
					{
						AddFace(chunk.blocks[i].index, sideLookup[j]);
					}
				}
			}
			Mesh mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.triangles = triangles.ToArray();
			mesh.RecalculateNormals();

			chunk.SetMesh(mesh, null);
			vertices.Clear();
			triangles.Clear();
		}

		public void AddFace(Vector3Int position, Vector3Int faceNormal)
		{
			var face = new Vector3[4];
			Vector3 offsetPosition = position - Vector3.one * 0.5f;
			// Calculate the rotated direction vectors based on the faceNormal
			Vector3 forward = Quaternion.LookRotation(faceNormal) * Vector3.forward;
			Vector3 right = Quaternion.LookRotation(faceNormal) * Vector3.right;
			Vector3 up = Quaternion.LookRotation(faceNormal) * Vector3.up;

			// Calculate the vertices based on the position and the rotated direction vectors
			face[0] = offsetPosition + forward - right - up;
			face[1] = offsetPosition + forward + right - up;
			face[2] = offsetPosition + forward + right + up;
			face[3] = offsetPosition + forward - right + up;


			triangles.Add(vertices.Count);     // 0
			triangles.Add(vertices.Count + 1); // 1
			triangles.Add(vertices.Count + 2); // 2

			triangles.Add(vertices.Count);     // 0
			triangles.Add(vertices.Count + 2); // 2
			triangles.Add(vertices.Count + 3); // 3

			vertices.AddRange(face);

			// TODO: Add UVs
		}
	}
}