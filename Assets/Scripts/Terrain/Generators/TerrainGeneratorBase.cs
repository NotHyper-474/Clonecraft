using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public abstract class TerrainGeneratorBase : MonoBehaviour, ITerrainGenerator
	{
		protected readonly FastNoise noise = new FastNoise();

		public virtual void SetSeed(int seed)
		{
			noise.SetSeed(seed);
		}

		public abstract BlockType CalculateBlockType(Vector3Int chunkSize, Vector3Int globalIndex);
		public abstract void GenerateBlocksFor(TerrainChunk chunk);
	}
}