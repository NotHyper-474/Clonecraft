using UnityEngine;

namespace Minecraft
{
    public class TerrainConfig : MonoBehaviour
    {
        public Vector3Int chunkSize = new (16, 256, 16);
        public float blockSize = 1f;
    }
}