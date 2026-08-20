using Unity.Jobs;

namespace Clonecraft
{
    public interface ITerrainMesherJob : IJob
    {
        void Dispose();
    }
}