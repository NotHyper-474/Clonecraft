using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	[System.Flags]
	public enum VoxelSides
	{
		None,
		SOUTH = 0, NORTH = 1,
		EAST = 2, WEST = 3,
		TOP = 4, BOTTOM = 5,
		ALL = SOUTH | NORTH | EAST | TOP
	}
	
	public struct VoxelFace : IEquatable<VoxelFace>
	{
		public VoxelFace(TerrainBlock block)
		{
			lightLevel = 0;
			type = (int)block.type;
			side = block.metadata;
			transparent = false;
		}

		public int lightLevel;
		public int type;
		public int side;
		public bool transparent;
		
		public bool Equals(VoxelFace other)
		{
			return other.transparent == transparent && other.type == type;
		}

		public bool IsEmpty() => type == 0;
	}
}