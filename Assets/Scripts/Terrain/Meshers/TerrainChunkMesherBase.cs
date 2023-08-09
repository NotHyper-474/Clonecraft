using System.Collections;
using UnityEngine;

namespace Minecraft
{
    public abstract class TerrainChunkMesherBase : ITerrainChunkMesher
    {
        public abstract void GenerateMeshFor(TerrainChunk chunk);
    }
}