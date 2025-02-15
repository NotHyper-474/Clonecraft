using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	[CreateAssetMenu(menuName = "Clonecraft/Meshers/Culled", fileName = "Culled Mesher")]
	public sealed class TerrainChunkMesherCulled : TerrainChunkMesherBase
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
			foreach (var side in _sideLookup)
			{
				for (int i = 0; i < chunk.Blocks.Length; i++)
				{
					if (chunk.Blocks[i].IsEmpty()) continue;

					var nextIndex = chunk.Blocks[i].index + side;
					var blockAtSide = chunk.GetBlock(nextIndex);
					if (!blockAtSide.HasValue && neighbours != null && neighbours.Length != 0)
					{
						nextIndex += chunk.Index;
						var adjChunkIdx = TerrainManager.Instance.GetChunkIndexAt(nextIndex);
						foreach (var chk in neighbours)
						{
							if (chk.Index == adjChunkIdx)
							{
								blockAtSide = chk.GetBlock(nextIndex - chunk.Index);
							}
						}
					}

					if (blockAtSide == null || blockAtSide.Value.IsEmpty())
						AddFace(chunk.Blocks[i].index, side);
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
			const float offset = 0.5f;
			var face = new Vector3[4];
			Vector3 offsetPosition = position;
			
			// Calculate the rotated direction vectors based on the faceNormal
			Vector3 forward = Quaternion.LookRotation(faceNormal) * Vector3.forward * offset;
			Vector3 right = Quaternion.LookRotation(faceNormal) * Vector3.right * offset;
			Vector3 up = Quaternion.LookRotation(faceNormal) * Vector3.up * offset;

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