using System.Runtime.CompilerServices;
using UnityEngine;
using Unity.Mathematics;

public static class MathUtils
{
	/// <summary>
	/// Takes 3D indexes and returns a 1D index based on them
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="xMax"></param>
	/// <param name="yMax"></param>
	/// <returns>1D index calculation</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int To1D(int x, int y, int z, int xMax, int yMax)
	{
		//return (z * xMax * yMax) + (y * xMax) + x;
		return x + xMax * (y + yMax * z);
	}

	/// <summary>
	/// Takes 2D indexes and returns a 1D index based on them
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="width"></param>
	/// <returns>1D index calculation</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int To1D(int x, int y, int width)
	{
		return y * width + x;
	}

	/// <summary>
	/// Takes 1D index and returns 3D indexes based on it
	/// </summary>
	/// <param name="index"></param>
	/// <param name="xMax"></param>
	/// <param name="yMax"></param>
	/// <returns></returns>
	public static Vector3Int To3D(int index, int xMax, int yMax)
	{
		int z = index / (xMax * yMax);
		int idx = index - z * xMax * yMax;
		int y = idx / xMax;
		int x = idx % xMax;
		return new Vector3Int(x, y, z);
	}

	public static Vector3Int WrapIndex(Vector3Int position, Vector3Int size)
	{
		return new Vector3Int(
			(position.x % size.x + size.x) % size.x,
			(position.y % size.y + size.y) % size.y,
			(position.z % size.z + size.z) % size.z
		);
	}

	public static Vector3Int GetNextChunkIndex(Vector3Int blockIndex, Vector3Int chunkPosition, Vector3Int chunkSize)
	{
		var result = chunkPosition;
		if (blockIndex.x >= chunkSize.x) result.x += 1;
		else if (blockIndex.x < 0) result.x -= 1;
		
		// TODO: Is Y necessary?
		
		if (blockIndex.z >= chunkSize.z) result.z += 1;
		else if (blockIndex.z < 0) result.z -= 1;

		return result;
	}
}
