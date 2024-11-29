using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    public sealed class BlockPlace : MonoBehaviour
    {
        [SerializeField] private Material material;
        [SerializeField] private GameObject dinamitePrefab;
        [SerializeField] private int selectedType;
        [SerializeField] private VoxelType[] types;

        private BlockFace? blockFace;
        private PlayerFPSController playerController;

        // Start is called before the first frame update
        private void Start()
        {
            playerController = transform.root.GetComponent<PlayerFPSController>();
        }

        private void OnGUI()
        {
            //GUI.Label(new Rect(5f, 35f, 200f, 25f), "Selected BT: " + System.Enum.GetName(typeof(BlockType), type));
            //GUI.Label(new Rect(5f, 50f, 250f, 25f), "SBT BlockFace Position: " + type.GetValueOrDefault().globalIndex);
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedType--;
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedType++;
            }

            selectedType = (selectedType % types.Length + types.Length) % types.Length;

            blockFace = null;

            Ray cRay = new Ray(transform.position, transform.forward);

            var pointOnTerrain = TerrainManager.Instance.RaycastTerrainMesh(cRay, TerrainManager.Instance.RayOffset);

            if (pointOnTerrain == null) return;

            var block = TerrainManager.Instance.GetBlockAt(pointOnTerrain.Point);

            if (block.type == VoxelType.Air) return;

            Debug.DrawLine(transform.position + Vector3.up * 0.3f, pointOnTerrain.Point, Color.green);
            Debug.DrawLine(transform.position + Vector3.up * 0.1f, block.globalIndex, new Color(1f, 0f, 0f, 0.5f));
            Bounds blockBounds = new Bounds(block.globalIndex, Vector3.one);
            Bounds playerBounds = playerController.Controller.bounds;
            playerBounds.Expand(new Vector3(0f, 1f, 0f));

            if (Input.GetMouseButtonDown(0))
            {
                TerrainManager.Instance.RemoveBlock(cRay);
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (!blockBounds.Intersects(playerBounds))
                {
                    TerrainManager.Instance.AddBlock(cRay, types[selectedType]);
                }
            }
            if (Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                if (!blockBounds.Intersects(playerBounds))
                {
                    Physics.Raycast(cRay, out RaycastHit hit);
                    Instantiate(dinamitePrefab, hit.point, Quaternion.identity);
                }
            }

            const float faceOffset = 1.005f;
            var blockFaceCenter = blockBounds.center +
                                  Vector3.Scale(pointOnTerrain.Normal, blockBounds.extents * faceOffset);
            blockFace = new BlockFace()
            {
                Center = blockFaceCenter,
                Normal = pointOnTerrain.Normal,
                BlockExtents = blockBounds.extents
            };
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
            GL.Color(new Color(1f, 1f, 1f, 0.2f));

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