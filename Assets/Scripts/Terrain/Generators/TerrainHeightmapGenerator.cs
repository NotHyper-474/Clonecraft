using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    [CreateAssetMenu(menuName = "Clonecraft/Generators/Heightmap")]
    public class TerrainHeightmapGenerator : TerrainGeneratorBase
    {
        public override VoxelType CalculateBlockType(Vector3Int chunkSize, Vector3Int i)
        {
            float n = noise.GetSimplex(i.x, i.y * 0.5f, i.z);

            if (n >= 0)
            {
                return VoxelType.Air;
            }
            else
            {
                return VoxelType.Stone;
            }
        }
    }
}