using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Clonecraft
{
	public interface ITerrainGenerator
	{
		void GenerateBlocksFor(TerrainChunk chunk);
		VoxelType CalculateBlockType(Vector3Int chunkSize, Vector3Int globalIndex);
	}
}