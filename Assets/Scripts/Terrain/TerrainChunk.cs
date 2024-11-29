﻿using System;
using UnityEngine;

namespace Minecraft
{
	public class TerrainChunk : MonoBehaviour
	{
		public Vector3Int index { get => _chunkIndex; private set => _chunkIndex = value; }
		public TerrainBlock[] blocks { get; private set; }

		public Vector3Int Size { get; private set; }

		public MeshFilter meshFilter { get; protected set; }
		public MeshCollider meshCollider { get; protected set; }
		public MeshRenderer meshRenderer { get; protected set; }

		[SerializeField]
		private Vector3Int _chunkIndex;

		private void Awake()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshCollider = GetComponent<MeshCollider>();
			meshRenderer = GetComponent<MeshRenderer>();
		}

		public void Setup(Vector3Int globalIndex, TerrainConfig config, bool fillEmpty = true)
		{
			index = globalIndex;
			Size = config.chunkSize;
			blocks = new TerrainBlock[Size.x * Size.y * Size.z];
			
			if (!fillEmpty) return;
			// Cycle through blocks to assign them empty data
			System.Threading.Tasks.Parallel.For(0, blocks.Length, i =>
			{
				var blockIndex = MathUtils.To3D(i, Size.x, Size.y);
				blocks[i] = new TerrainBlock(VoxelType.Air, blockIndex, globalIndex: index + blockIndex, 0);
			});
			//firstBlockGlobalIndex = index * Size - Vector3Int.one;
		}

		public void SetMesh(Mesh mesh, Material material)
		{
			if (!mesh && meshFilter.mesh)
				Destroy(meshFilter.mesh);
			meshFilter.mesh = mesh;
			
			if (meshCollider) meshCollider.sharedMesh = mesh;
			if (!material) return;
			meshRenderer.material = material;
		}

		public void SetBlockType(Vector3Int localIndex, VoxelType type)
		{
			blocks[MathUtils.To1D(localIndex.x, localIndex.y, localIndex.z, Size.x, Size.y)].type = type;
		}

		public TerrainBlock? GetBlock(int x, int y, int z)
		{
			if (x >= 0 && y >= 0 && z >= 0 && x < Size.x && y < Size.y && z < Size.z)
				return blocks[MathUtils.To1D(x, y, z, Size.x, Size.y)];

			return null;
		}

		public TerrainBlock? GetBlock(Vector3Int position)
		{
			return GetBlock(position.x, position.y, position.z);
		}

		public static TerrainBlock GetEmptyBlock(Vector3Int index, Vector3Int globalIndex)
		{
			return new TerrainBlock(VoxelType.Air, index, globalIndex, 0b0);
		}
	}
}