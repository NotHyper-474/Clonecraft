using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Minecraft
{
	[CreateAssetMenu(fileName = "New TerrainSimplexGenerator", menuName = "Clonecraft/Generators/Simplex", order=1)]
	public sealed class TerrainSimplexGenerator : TerrainGeneratorBase
	{
		// Totally not stolen from Sam Hogan (check him out he does some cool stuff)
		public override VoxelType CalculateBlockType(Vector3Int chunkSize, Vector3Int blockGlobalIndex)
		{
			int x, y, z;
			(x, y, z) = (blockGlobalIndex.x, blockGlobalIndex.y, blockGlobalIndex.z);


			float simplex1 = noise.GetSimplex(x * .8f, z * .8f) * 10;
			float simplex2 = noise.GetSimplex(x * 3f, z * 3f) * 10 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

			float heightMap = simplex1 + simplex2;

			//add the 2d noise to the middle of the terrain chunk
			float baseLandHeight = chunkSize.y * .5f + heightMap;

			//3d noise for caves and overhangs and such
			float caveNoise1 = noise.GetPerlinFractal(x * 5f, y * 10f, z * 5f);
			float caveMask = noise.GetSimplex(x * .3f, z * .3f) + .3f;

			//stone layer heightmap
			float simplexStone1 = noise.GetSimplex(x * 1f, z * 1f) * 10;
			float simplexStone2 = (noise.GetSimplex(x * 5f, z * 5f) + .5f) * 20 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

			float stoneHeightMap = simplexStone1 + simplexStone2;
			float baseStoneHeight = chunkSize.y * .25f + stoneHeightMap;


			//float cliffThing = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
			//float cliffThingMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;

			VoxelType blockType = VoxelType.Air;

			//under the surface, dirt block
			if (y <= baseLandHeight)
			{
				blockType = VoxelType.Dirt;

				//just on the surface, use a grass type
				if (y > baseLandHeight - 1 && y > /*WaterChunk.waterHeight*/ 28 - 2)
					blockType = VoxelType.Grass;

				if (y <= baseStoneHeight)
					blockType = VoxelType.Stone;
			}


			if (caveNoise1 > Mathf.Max(caveMask, .2f))
				blockType = VoxelType.Air;

			/*if(blockType != BlockType.Air)
				blockType = BlockType.Stone;*/

			//if(blockType == BlockType.Air && noise.GetSimplex(x * 4f, y * 4f, z*4f) < 0)
			//  blockType = BlockType.Dirt;

			//if(Mathf.PerlinNoise(x * .1f, z * .1f) * 10 + y < TerrainChunk.chunkHeight * .5f)
			//    return BlockType.Grass;

			return blockType;
		}
	}
}
