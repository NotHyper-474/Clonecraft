using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
	[Flags]
	public enum VoxelSides
	{
		None,
		SOUTH = 0, NORTH = 1,
		EAST = 2, WEST = 3,
		TOP = 4, BOTTOM = 5,
		ALL = SOUTH | NORTH | EAST | TOP
	}
	
	/// <summary>
	/// Mesher Block
	/// </summary>
	public struct MaskBlock : IEquatable<MaskBlock>
	{
		public int type;
		public int normal;
		public VoxelSides shownSides;
		
		public bool Equals(MaskBlock other)
		{
			return other.normal == normal && other.type == type;
		}

		public bool IsEmpty() => type == 0;
	}
}