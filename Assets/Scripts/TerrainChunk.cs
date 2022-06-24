using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public class TerrainChunk : MonoBehaviour
	{
		public Vector3Int index { get; private set; }
		public TerrainBlock[] blocks { get; private set; }

		public Vector3Int Size { get; private set; }

		public MeshFilter meshFilter { get; protected set; }
		public MeshCollider meshCollider { get; protected set; }
		public MeshRenderer meshRenderer { get; protected set; }

		public static int mem;

		private void Awake()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshCollider = GetComponent<MeshCollider>();
			meshRenderer = GetComponent<MeshRenderer>();
		}

		public void Setup(Vector3Int globalIndex, Vector3Int chunkSize, bool fillEmpty = true)
		{
			index = globalIndex;
			Size = chunkSize;
			blocks = new TerrainBlock[chunkSize.x * chunkSize.y * chunkSize.z];

			// Cycle through blocks to assign them empty data
			for (int i = 0; i < blocks.Length; i++)
			{
				var blockIndex = MMMath.To3D(i, Size.x, Size.y);
				if (mem < 2)
					Debug.Log(blockIndex);
				blocks[i] = new TerrainBlock(BlockType.Air, blockIndex, index + blockIndex, 0);
			}
			mem++;
			//firstBlockGlobalIndex = index * Size - Vector3Int.one;
		}

		[Obsolete()]
		public static TerrainChunk SetupAndInstantiate(Vector3Int globalIndex, Vector3Int chunkSize, Transform parent)
		{
			GameObject go = new GameObject("CHUNK", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(TerrainChunk));
			go.transform.position = new Vector3(globalIndex.x * chunkSize.x, globalIndex.y * chunkSize.y, globalIndex.z * chunkSize.z);
			go.transform.SetParent(parent);
			go.GetComponent<TerrainChunk>().Setup(globalIndex, chunkSize);

			return go.GetComponent<TerrainChunk>();
		}

		public void SetMesh(Mesh mesh, Material material)
		{
			meshFilter.mesh = mesh;
			if (meshCollider != null) meshCollider.sharedMesh = mesh;
			if (material == null) return;
			meshRenderer.material = material;
		}

		public void SetBlockType(Vector3Int localIndex, BlockType type)
		{
			blocks[MMMath.FlattenIndex(localIndex.x, localIndex.y, localIndex.z, Size.x, Size.y)].type = type;
		}

		public TerrainBlock GetBlock(int x, int y, int z)
		{
			//Debug.Log("true at " + new Vector3Int(x, y, z));
			if (x < 0 || y < 0 || z < 0 || x > Size.x || y > Size.y || z > Size.z)
			{
				var ind = new Vector3Int(x, y, z);
				return GetEmptyBlock(ind, ind * index);
			}
			return blocks[MMMath.FlattenIndex(x, y, z, Size.x, Size.y)];
		}

		public TerrainBlock GetBlock(Vector3Int position)
		{
			return GetBlock(position.x, position.y, position.z);
		}

		public static TerrainBlock GetEmptyBlock(Vector3Int index, Vector3Int globalIndex)
		{
			return new TerrainBlock(BlockType.Air, index, globalIndex, 0b0);
		}
	}
}