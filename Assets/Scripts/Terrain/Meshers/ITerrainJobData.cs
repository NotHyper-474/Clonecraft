using Unity.Jobs;

namespace Minecraft
{
    /// <summary>
    /// Generic interface for type-safe job data with specific job type.
    /// </summary>
    public interface ITerrainJobData
    {
        ITerrainMesherJob Job { get; set; }
        JobHandle Handle { get; set; }
    }
}