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
            public int layerYMin;
            public int layerYMax;
            public BlockType blockType;
        }

        public int landHeight = 4;
        public LandLayer[] layers;

        public override BlockType CalculateBlockType(Vector3Int chunkSize, Vector3Int globalIndex)
        {
            if (globalIndex.y > landHeight)
            {
				return BlockType.Air;
            }
            for (int j = 0; j < layers.Length; j++)
            {
                if ((globalIndex.y - (layers[j].layerYMax - layers[j].layerYMin)) >= 0)
                    return layers[j].blockType;
            }
			return BlockType.Air;
        }
    }
}
