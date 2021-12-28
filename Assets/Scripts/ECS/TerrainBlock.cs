using Unity.Mathematics;

namespace Minecraft.DOTS
{
	public struct TerrainBlock
	{
		public int3 index;
		public int3 globalIndex;
		public BlockType blockType;
		public byte metadata;

		//public bool IsEmpty() => blockType == BlockType.Air;
	}
}