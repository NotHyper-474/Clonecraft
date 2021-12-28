using Minecraft;
using System.Threading.Tasks;
using UnityEngine;

public class UVTest : MonoBehaviour
{
	public TerrainChunk chunk;
	public TerrainBlock[] blocks;
	public Vector3Int chunkSize = new Vector3Int(16, 256, 16);
	public Vector2[] uvTest;

	private ChunkMeshBuilder builder;

	private bool chunkUpdate;
	private bool firstTime = true;

	private void Start()
	{
		builder = new ChunkMeshBuilder(null);
		chunk.Setup(Vector3Int.zero, chunkSize, false);
	}

	void Update()
	{
		if (!chunkUpdate)
		{
			StartCoroutine(ChunkUpdate());
		}
	}

	System.Collections.IEnumerator ChunkUpdate()
	{
		chunkUpdate = true;
		FastNoise noise = new FastNoise();
		//if (firstTime)
		{
			for (int i = 0; i < chunk.Size.x * chunk.Size.y * chunk.Size.z; i++)
			{
				chunk.blocks[i] = blocks[i];
				//chunk.blocks[i] = GetBlockTypeAt(noise, MMMath.To3D(i, chunk.Size.x, chunk.Size.y));
			};
			firstTime = false;
		}
		builder.Clear();
		Mesh m = builder.JobsBuildChunk(chunk, uvTest);
		m.Optimize();
		chunk.SetMesh(m, null);
		yield return new WaitForSeconds(1f);
		chunkUpdate = false;
		yield break;
	}

	private TerrainBlock GetBlockTypeAt(FastNoise noise, Vector3Int blockGlobalIndex)
	{
		int x, y, z;
		(x, y, z) = (blockGlobalIndex.x, blockGlobalIndex.y, blockGlobalIndex.z);


		float simplex1 = noise.GetSimplex(x * .8f, z * .8f) * 10;
		float simplex2 = noise.GetSimplex(x * 3f, z * 3f) * 10 * (noise.GetSimplex(x * .3f, z * .3f) + .2f);

		float heightMap = simplex1 + simplex2;

		//add the 2d noise to the middle of the terrain chunk
		float baseLandHeight = chunk.Size.y * .2f + heightMap;

		//3d noise for caves and overhangs and such
		float caveNoise1 = noise.GetPerlinFractal(x * 5f, y * 10f, z * 5f);
		float caveMask = noise.GetSimplex(x * .3f, z * .3f) + .3f;

		//stone layer heightmap
		float simplexStone1 = noise.GetSimplex(x * 1f, z * 1f) * 10;
		float simplexStone2 = (noise.GetSimplex(x * 5f, z * 5f) + .5f) * 20 * (noise.GetSimplex(x * .3f, z * .3f) + .5f);

		float stoneHeightMap = simplexStone1 + simplexStone2;
		float baseStoneHeight = chunkSize.y * .15f + stoneHeightMap;


		//float cliffThing = noise.GetSimplex(x * 1f, z * 1f, y) * 10;
		//float cliffThingMask = noise.GetSimplex(x * .4f, z * .4f) + .3f;


		TerrainBlock blockType = new TerrainBlock(BlockType.Air, blockGlobalIndex - (chunk.index * chunk.Size), blockGlobalIndex, 0);

		//under the surface, dirt block
		if (y <= baseLandHeight)
		{
			blockType.type = BlockType.Dirt;

			//just on the surface, use a grass type
			if (y > baseLandHeight - 1 && y > /*WaterChunk.waterHeight*/ 62 - 2)
				blockType.type = BlockType.Grass;

			if (y <= baseStoneHeight)
				blockType.type = BlockType.Stone;
		}


		if (caveNoise1 > Mathf.Max(caveMask, .2f))
			blockType.type = BlockType.Air;

		return blockType;
	}
}
