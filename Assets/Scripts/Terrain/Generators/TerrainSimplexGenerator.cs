using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Clonecraft
{
	[CreateAssetMenu(fileName = "New TerrainSimplexGenerator", menuName = "Clonecraft/Generators/Simplex", order=1)]
	public sealed class TerrainSimplexGenerator : TerrainGeneratorBase
	{
		// Totally not stolen from Sam Hogan (check him out he does some cool stuff)
		public override VoxelType CalculateBlockType(Vector3Int chunkSize, Vector3Int blockGlobalIndex)
		{
			var (x, y, z) = (blockGlobalIndex.x, blockGlobalIndex.y, blockGlobalIndex.z);

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
			float baseStoneHeight = chunkSize.y * .35f + stoneHeightMap;
			
			//float cliffThing = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
			//float cliffThingMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;

			// under the surface, dirt block
			if (y <= baseLandHeight)
			{
				//just on the surface, use a grass type
				if (y > baseLandHeight - 1 && y > /*WaterChunk.waterHeight*/ 28 - 2)
					return VoxelType.Grass;

				if (caveNoise1 > Mathf.Max(caveMask, .2f))
					return VoxelType.Air;
				if (y <= baseStoneHeight)
					return VoxelType.Stone;

				return VoxelType.Dirt;
			}

			return VoxelType.Air;
		}
	}
}
