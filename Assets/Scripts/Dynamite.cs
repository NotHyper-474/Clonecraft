using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Minecraft
{
	public class Dynamite : MonoBehaviour
	{
		[SerializeField] private float explosionRadius = 5f;
		
		private bool _triggered;

		private async Task OnCollisionEnter(Collision _)
		{
			if (_triggered) return;
			_triggered = true;
			await Task.Delay(3000);
			Stack<TerrainChunk> chunksToRegenerate = new Stack<TerrainChunk>();
			foreach (var (chunk, block) in GetBlocksInSphere(transform.position, explosionRadius))
			{
				chunk.SetBlockType(block.index, VoxelType.Air);
				chunksToRegenerate.Push(chunk);
			}
			// We cannot destroy it yet or else nearby chunks won't regenerate their mesh
			gameObject.SetActive(false);
			while (chunksToRegenerate.Count > 0)
			{
				var chunk = chunksToRegenerate.Pop();
				TerrainManager.Instance.BuildMeshForChunk(chunk);
				await Task.Yield();
			}
			
			Destroy(gameObject);
		}
		
		private IEnumerable<Tuple<TerrainChunk, TerrainBlock>> GetBlocksInSphere(Vector3 sphereCenter, float radius)
		{
			Vector3 minPoint = sphereCenter - Vector3.one * radius;
			Vector3 maxPoint = sphereCenter + Vector3.one * radius;

			Vector3Int minIndex = Vector3Int.FloorToInt(minPoint);
			Vector3Int maxIndex = Vector3Int.FloorToInt(maxPoint);

			float sphereRadius2 = radius * radius;

			for (int x = minIndex.x; x <= maxIndex.x; x++)
			{
				for (int y = minIndex.y; y <= maxIndex.y; y++)
				{
					for (int z = minIndex.z; z <= maxIndex.z; z++)
					{
						Vector3 blockCenter = new Vector3(x, y, z);
						float dis = Vector3.SqrMagnitude(blockCenter - sphereCenter);
						if (dis > sphereRadius2) continue;

						var block = TerrainManager.Instance.GetBlockAt(blockCenter, out TerrainChunk chunk);
						if (!block.IsEmpty())
						{
							yield return new Tuple<TerrainChunk, TerrainBlock>(chunk, block);
						}
					}
				}
			}
		}
	}
}