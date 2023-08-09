using System;
using UnityEngine;

namespace Minecraft
{
	public enum VoxelType : byte
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
		public TerrainBlock(VoxelType type, Vector3Int index, Vector3Int globalIndex, byte metadata)
		{
			this.type = type;
			this.index = index;
			this.globalIndex = globalIndex;
			this.metadata = metadata;
		}

		public Vector3Int index;
		public Vector3Int globalIndex;
		public VoxelType type;
		public byte metadata; // One byte for some info the block might need

		public readonly bool IsEmpty() => type == VoxelType.Air;

		public readonly bool Equals(TerrainBlock obj)
		{
			return obj.type == type;
		}
	}
}