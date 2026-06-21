using UnityEngine;

namespace Minecraft
{
    public abstract class TerrainChunkMesherBase : ScriptableObject, ITerrainChunkMesher
    {
        public abstract ITerrainJobData GenerateMeshFor(TerrainChunk chunk, TerrainChunk[] neighbors);
        public abstract void ApplyData(ITerrainJobData data);
    }
}