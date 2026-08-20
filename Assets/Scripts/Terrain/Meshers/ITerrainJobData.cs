using Unity.Jobs;

namespace Clonecraft
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