using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public class Dinamite : MonoBehaviour
	{
		public TerrainManager manager;

		private void Start()
		{
			manager = FindObjectOfType<TerrainManager>();
		}

        private void OnCollisionEnter(Collision other)
		{
			//yield return new WaitForSeconds(3f);
			foreach (var entry in GetBlocksInSphere(transform.position, 5f))
			{
				TerrainChunk chunk = entry.Item1;
				TerrainBlock block = entry.Item2;

				chunk.SetBlockType(block.index, BlockType.Air);

				manager.BuildMeshForChunk(chunk);
			}
			Destroy(gameObject);
		}
		
		private IEnumerable<Tuple<TerrainChunk, TerrainBlock>> GetBlocksInSphere(Vector3 sphereCenter, float radius)
		{
			Vector3 minPoint = sphereCenter - Vector3.one * radius;
			Vector3 maxPoint = sphereCenter + Vector3.one * radius;

			Vector3Int minIndex = MMMath.FloorToInt3D(minPoint);
			Vector3Int maxIndex = MMMath.FloorToInt3D(maxPoint);

			float sphereRadius2 = radius * radius;

			for (int x = minIndex.x; x <= maxIndex.x; x++)
			{
				for (int y = minIndex.y; y <= maxIndex.y; y++)
				{
					for (int z = minIndex.z; z < maxIndex.z; z++)
					{
						Vector3 blockCenter = new Vector3(x, y, z);
						float dis = Vector3.SqrMagnitude(blockCenter - sphereCenter);

						if (dis <= sphereRadius2)
						{
							var block = manager.GetBlockAt(blockCenter, out TerrainChunk chunk);

							if (block != null && !block.Value.IsEmpty())
							{
								yield return new Tuple<TerrainChunk, TerrainBlock>(chunk, block.Value);
							}
						}
					}
				}
			}
		}
	}
}