using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minecraft
{
    public interface ITerrainChunkMesher
    {
        void GenerateMeshFor(TerrainChunk chunk);
    }
}