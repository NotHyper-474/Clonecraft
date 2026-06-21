using Unity.Jobs;

namespace Minecraft
{
    public interface ITerrainMesherJob : IJob
    {
        void Dispose();
    }
}