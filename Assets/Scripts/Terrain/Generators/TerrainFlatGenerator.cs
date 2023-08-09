using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    [CreateAssetMenu(menuName = "Clonecraft/Generators/Flat", order = 1)]
    public class TerrainFlatGenerator : TerrainGeneratorBase
    {
		[System.Serializable]
        public struct LandLayer
        {
            public int height;
            public VoxelType blockType;
        }
        public LandLayer[] layers;

        public override VoxelType CalculateBlockType(Vector3Int chunkSize, Vector3Int globalIndex)
        {
            int totalHeight = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                totalHeight += layers[i].height;
                if (globalIndex.y < totalHeight) {
                    return layers[i].blockType;
                }
            }
			return VoxelType.Air;
        }
    }
}
