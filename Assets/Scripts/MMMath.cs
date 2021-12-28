using UnityEngine;
using Unity.Mathematics;

public static class MMMath
{
	/// <summary>
	/// Takes 3D indexes and returns a 1D index based on them
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="xMax"></param>
	/// <param name="yMax"></param>
	/// <returns>1D index calulation</returns>
	public static int FlattenIndex(int x, int y, int z, int xMax, int yMax)
	{
		//return (z * xMax * yMax) + (y * xMax) + x;
		return x + xMax * (y + yMax * z);
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
		int idx = index - (z * xMax * yMax);
		int y = idx / xMax;
		int x = idx % xMax;
		return new Vector3Int(x, y, z);
	}

	public static int3 To3D(long index, int xMax, int yMax)
	{
		int z = (int)index / (xMax * yMax);
		int idx = (int)index - (z * xMax * yMax);
		int y = idx / xMax;
		int x = idx % xMax;
		return new int3(x, y, z);
	}

	public static Vector3Int FloorToInt3D(Vector3 v)
	{
		return new Vector3Int(
			Mathf.FloorToInt(v.x),
			Mathf.FloorToInt(v.y),
			Mathf.FloorToInt(v.z)
			);
	}
	
	public static T FloorToInt3D<T>(int3 v)
	{
		return (T)(object)new Vector3Int(
			Mathf.FloorToInt(v.x),
			Mathf.FloorToInt(v.y),
			Mathf.FloorToInt(v.z)
			);
	}

	public static Vector3Int FloorToInt3D(float x, float y, float z)
	{
		return new Vector3Int(
			Mathf.FloorToInt(x),
			Mathf.FloorToInt(y),
			Mathf.FloorToInt(z)
			);
	}

	public static Vector3Int CeilToInt3D(Vector3 v)
	{
		return new Vector3Int(
			Mathf.CeilToInt(v.x),
			Mathf.CeilToInt(v.y),
			Mathf.CeilToInt(v.z)
			);
	}
	public static Vector3Int CeilToInt3D(float x, float y, float z)
	{
		return new Vector3Int(
			Mathf.CeilToInt(x),
			Mathf.CeilToInt(y),
			Mathf.CeilToInt(z)
			);
	}
}
