using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public sealed class BlockPlace : MonoBehaviour
	{
		[SerializeField]
		private Material material;
		[SerializeField] private GameObject dinamitePrefab;

		public TerrainManager manager;

		private BlockFace? blockFace;
		private BlockType type;
		private PlayerFPSController playerController;

		// Start is called before the first frame update
		private void Start()
		{
			playerController = transform.root.GetComponent<PlayerFPSController>();
		}

		private void OnGUI()
		{
			GUI.Label(new Rect(5f, 35f, 200f, 25f), "Selected BT: " + System.Enum.GetName(typeof(BlockType), type));
			//GUI.Label(new Rect(5f, 50f, 250f, 25f), "SBT BlockFace Position: " + type.GetValueOrDefault().globalIndex);
		}

		// Update is called once per frame
		private void Update()
		{
			blockFace = null;

			Ray cRay = new Ray(transform.position, transform.forward);

			var pointOnTerrain = manager.RaycastTerrainMesh(cRay, manager._RayOffset);

			if (pointOnTerrain != null)
			{
				var block = manager.GetBlockAt(pointOnTerrain.Point);
				if (block != null) type = block.Value.type;

				if (block != null && block.Value.type != BlockType.Air)
				{
					Debug.DrawLine(transform.position + Vector3.up * 0.3f, pointOnTerrain.Point, Color.green);
					Debug.DrawLine(transform.position + Vector3.up * 0.1f, block.Value.globalIndex, new Color(1f, 0f, 0f, 0.5f));
					Bounds blockBounds = new Bounds(block.Value.globalIndex, Vector3.one);

					if (Input.GetMouseButtonDown(0))
					{
						manager.RemoveBlock(cRay);
					}
					else if (Input.GetMouseButtonDown(1))
					{
						if(!blockBounds.Intersects(playerController.Controller.bounds))
						{
							manager.AddBlock(cRay, BlockType.Grass);
						}
					}
					else if (Input.GetMouseButtonDown(2))
					{
						if (!blockBounds.Intersects(playerController.Controller.bounds))
						{
							Physics.Raycast(cRay, out RaycastHit hit);
							Instantiate(dinamitePrefab, hit.point, Quaternion.identity);
						}
					}

					const float faceOffset = 1.001f;

					var blockFaceCenter = blockBounds.center + Vector3.Scale(pointOnTerrain.Normal, blockBounds.extents * faceOffset);

					blockFace = new BlockFace()
					{
						Center = blockFaceCenter,
						Normal = pointOnTerrain.Normal,
						BlockExtents = blockBounds.extents
					};
				}
			}
		}

		private void OnPostRender()
		{
			if (blockFace != null)
			{
				var faceVertices = GetFaceVertices(blockFace.Value);
				DrawFace(faceVertices);
			}
		}

		private static IEnumerable<Vector3> GetFaceVertices(BlockFace face)
		{
			GetFaceAxis(face, out Vector3 right, out Vector3 up);

			var center = face.Center;

			return new Vector3[]
			{
				center - right - up,
				center - right + up,
				center + right + up,
				center + right - up,
			};
		}

		private static void GetFaceAxis(BlockFace face, out Vector3 right, out Vector3 up)
		{
			var normal = face.Normal;
			right = Vector3.Cross(normal, Vector3.up);

			if (right.sqrMagnitude < 0.01f)
			{
				right = Vector3.Cross(normal, Vector3.forward);
			}

			up = Vector3.Cross(right, normal);

			right = Vector3.Scale(right, face.BlockExtents);
			up = Vector3.Scale(up, face.BlockExtents);
		}

		private void DrawFace(IEnumerable<Vector3> vertices)
		{
			material.SetPass(0);

			GL.Begin(GL.QUADS);
			GL.Color(new Color(0f, 0f, 0f, 0.2f));

			foreach (var vertex in vertices)
			{
				GL.Vertex(vertex);
			}

			GL.End();
		}

		private struct BlockFace
		{
			public Vector3 Center { get; set; }
			public Vector3 Normal { get; set; }
			public Vector3 BlockExtents { get; set; }
		}
	}
}