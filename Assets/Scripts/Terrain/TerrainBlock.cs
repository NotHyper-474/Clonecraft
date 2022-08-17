using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	public enum BlockType : byte
	{
		Air, // Or None
		Grass,
		Dirt,
		Stone,
		OakLog
	}

	[Serializable]
	public struct TerrainBlock : IEquatable<TerrainBlock>
	{
		public TerrainBlock(BlockType type, Vector3Int index, Vector3Int globalIndex, byte metadata)
		{
			this.type = type;
			this.index = index;
			this.globalIndex = globalIndex;
			this.metadata = metadata;
		}

		public Vector3Int index;
		public Vector3Int globalIndex;
		public BlockType type;
		public byte metadata; // One byte for some info the block might need

		public bool IsEmpty() => type == BlockType.Air;

		public bool Equals(TerrainBlock obj)
		{
			return obj.type == type;
		}
	}
}