using System.Collections;
using UnityEngine;

namespace Minecraft
{
    public abstract class TerrainChunkMesherBase : ScriptableObject, ITerrainChunkMesher
    {
        public abstract void GenerateMeshFor(TerrainChunk chunk, TerrainChunk[] neighbors = null);
    }
}