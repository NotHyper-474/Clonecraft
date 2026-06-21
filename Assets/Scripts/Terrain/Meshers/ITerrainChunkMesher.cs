
namespace Minecraft
{
    public interface ITerrainChunkMesher
    {
        ITerrainJobData GenerateMeshFor(TerrainChunk chunk, TerrainChunk[] neighbors);
        void ApplyData(ITerrainJobData data);
    }
}