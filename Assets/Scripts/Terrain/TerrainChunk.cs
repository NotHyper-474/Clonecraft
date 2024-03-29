﻿using System;
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
			
			if (!fillEmpty) return;
			// Cycle through blocks to assign them empty data
			System.Threading.Tasks.Parallel.For(0, blocks.Length, i =>
			{
				var blockIndex = MathUtils.To3D(i, Size.x, Size.y);
				blocks[i] = new TerrainBlock(VoxelType.Air, blockIndex, index + blockIndex, 0);
			});
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
			if (mesh == null)
				meshFilter.mesh.Clear();
			meshFilter.mesh = mesh;
			
			if (meshCollider != null) meshCollider.sharedMesh = mesh;
			if (material == null) return;
			meshRenderer.material = material;
		}

		public void SetBlockType(Vector3Int localIndex, VoxelType type)
		{
			blocks[MathUtils.To1D(localIndex.x, localIndex.y, localIndex.z, Size.x, Size.y)].type = type;
		}

		public TerrainBlock GetBlock(int x, int y, int z)
		{
			//Debug.Log("true at " + new Vector3Int(x, y, z));
			if (x < 0 || y < 0 || z < 0 || x >= Size.x || y >= Size.y || z >= Size.z)
			{
				var ind = new Vector3Int(x, y, z);
				return GetEmptyBlock(ind, ind * index);
			}
			return blocks[MathUtils.To1D(x, y, z, Size.x, Size.y)];
		}

		public TerrainBlock GetBlock(Vector3Int position)
		{
			return GetBlock(position.x, position.y, position.z);
		}

		public static TerrainBlock GetEmptyBlock(Vector3Int index, Vector3Int globalIndex)
		{
			return new TerrainBlock(VoxelType.Air, index, globalIndex, 0b0);
		}
	}
}