using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public class TerrainChunkMesherCulled : TerrainChunkMesherBase
	{
		private readonly List<Vector3> _vertices = new();
		private readonly List<int> _triangles = new();

		private readonly Vector3Int[] _sideLookup = {
			Vector3Int.left, // West
			Vector3Int.right , // East
			Vector3Int.up , // Top
			Vector3Int.down, // Bottom
			Vector3Int.forward, // North
			Vector3Int.back, // South
		};

		private static Dictionary<Vector3Int, Vector3[]> _faceSideCache = new();

		public override void GenerateMeshFor(TerrainChunk chunk, TerrainChunk[] neighbours = null)
		{
			for (int j = 0; j < _sideLookup.Length; j++)
			{
				for (int i = 0; i < chunk.blocks.Length; i++)
				{
					if (chunk.blocks[i].IsEmpty()) continue;

					var nextIndex = chunk.blocks[i].index + _sideLookup[j];
					var blockAtSide = chunk.GetBlock(nextIndex);
					if (!blockAtSide.HasValue && neighbours != null && neighbours.Length != 0)
					{
						nextIndex += chunk.index;
						var adjChunkIdx = TerrainManager.Instance.GetChunkIndexAt(nextIndex);
						foreach (var chk in neighbours)
						{
							if (chk.index == adjChunkIdx)
							{
								blockAtSide = chk.GetBlock(nextIndex - chunk.index);
							}
						}
					}

					if (blockAtSide?.IsEmpty() == false)
						AddFace(chunk.blocks[i].index, _sideLookup[j]);
				}
			}
			Mesh mesh = new Mesh
			{
				vertices = _vertices.ToArray(),
				triangles = _triangles.ToArray()
			};
			mesh.RecalculateNormals();

			chunk.SetMesh(mesh, null);
			_vertices.Clear();
			_triangles.Clear();
		}

		private void AddFace(Vector3Int position, Vector3Int faceNormal)
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


			_triangles.Add(_vertices.Count);     // 0
			_triangles.Add(_vertices.Count + 1); // 1
			_triangles.Add(_vertices.Count + 2); // 2

			_triangles.Add(_vertices.Count);     // 0
			_triangles.Add(_vertices.Count + 2); // 2
			_triangles.Add(_vertices.Count + 3); // 3

			_vertices.AddRange(face);

			// TODO: Add UVs
		}
	}
}