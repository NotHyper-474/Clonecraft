using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Collections;

public struct Vector3ElementData : IBufferElementData
{
	public Vector3 Value;
}

public struct IntElementData : IBufferElementData
{
	public int Value;
}

public struct Vector2ElementData : IBufferElementData
{
	public Vector2 Value;
}

public struct MeshData
{
	public MeshData(short ignoreThisVar)
	{
		vertices = new NativeArray<Vector3> (0, Allocator.Persistent);
		triangles = new NativeArray<int>	(0, Allocator.Persistent);
		uv = new NativeArray<Vector2>		(0, Allocator.Persistent);
	}

	public NativeArray<Vector3> vertices;
	public NativeArray<int> triangles;
	public NativeArray<Vector2> uv;

	public static explicit operator Mesh(MeshData data)
	{
		return new Mesh() {
			vertices = data.vertices.ToArray(),
			triangles = data.triangles.ToArray(),
			uv = data.uv.ToArray()
			};
	}

	// Removed due to allocation type issues
	/*public static implicit operator MeshData(Mesh mesh)
	{
		var vert = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob);
		var tri = new NativeArray<int>(mesh.triangles, Allocator.TempJob);
		var uv0 = new NativeArray<Vector2>(mesh.uv, Allocator.TempJob);

		MeshData result =  new MeshData()
		{
			vertices = vert,
			triangles = tri,
			uv = uv0
		};

		vert.Dispose();
		tri.Dispose();
		uv0.Dispose();

		return result;
	}*/
}