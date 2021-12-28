using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;

namespace Minecraft.DOTS
{
	public class TerrainManager : MonoBehaviour
	{
		public int3 chunkSize = new int3(16, 256, 16);
		public Mesh blockMesh;
		public Material blockMaterial;

		private void Start()
		{
			EntityManager manager = World.DefaultGameObjectInjectionWorld.EntityManager;
			Entity entity = manager.CreateEntity();

			DynamicBuffer<TerrainBlockElementData> tempBuffer = manager.AddBuffer<TerrainBlockElementData>(entity);
			NativeArray<TerrainBlockElementData> tempBlockData =
				new NativeArray<TerrainBlockElementData>(chunkSize.x * chunkSize.y * chunkSize.z, Allocator.Persistent);
			tempBuffer.AddRange(tempBlockData);
			
			tempBlockData.Dispose();

			var chunk = new TerrainChunkData()
			{
				blocks = tempBuffer.Reinterpret<TerrainBlock>(),
				Index = new int3(0 * chunkSize.x, 0, 0),
				Size = chunkSize
			};

			NativeArray<TerrainBlock> tempBlockData2 = new NativeArray<TerrainBlock>(chunk.blocks.AsNativeArray(), Allocator.TempJob);
			Mesh.MeshDataArray meshArray = Mesh.AcquireReadOnlyMeshData(blockMesh);
			TerrainChunkSystem.BlockJob job = new TerrainChunkSystem.BlockJob()
			{
				blockMesh = meshArray[0],
				chunk = chunk,
				blocks = tempBlockData2,
				vertices = new NativeList<Vector3>(Allocator.TempJob),
				triangles = new NativeList<int>(Allocator.TempJob),
				uvs = new NativeList<Vector2>(Allocator.TempJob)
			};
			job.Run(chunk.Length);

			Mesh mesh = (Mesh)new MeshData() { vertices = job.vertices, triangles = job.triangles, uv = job.uvs };
			for (int i = 0; i < tempBuffer.Length; i++)
			{
				tempBuffer[i] = new TerrainBlockElementData() { Value = job.blocks[i] };
			}
			chunk.blocks = tempBuffer.Reinterpret<TerrainBlock>();
			mesh.RecalculateNormals();

			job.Dispose();
			tempBlockData2.Dispose();
			meshArray.Dispose();

			gameObject.AddComponent<MeshFilter>().mesh = mesh;
			gameObject.AddComponent<MeshRenderer>().material = blockMaterial;

			//manager.DestroyEntity(entity);
		}
	}
}
