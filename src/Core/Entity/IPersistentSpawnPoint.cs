namespace Core.Entity
{
    using System.Collections.Generic;
    using Data.SaveData;

    /// <summary>
    /// A spawn point whose population state survives save/load. Without it every scene load
    /// refilled the points to capacity — saving next to a camp, killing everyone and reloading
    /// respawned the full roster instantly (the save-scum loot exploit).
    /// </summary>
    public interface IPersistentSpawnPoint
    {
        /// <summary>Stable save identity of the point.</summary>
        string PointId { get; }

        SpawnPointSaveData CaptureState();

        /// <summary>Load path: recreate the saved alive count and resume the respawn timers.</summary>
        void RestoreState(SpawnPointSaveData data);

        /// <summary>No saved data for this point (fresh game or a point added later): fill normally.</summary>
        void FillFresh();
    }

    /// <summary>Scene-side points register here; the save participant walks the registry.</summary>
    public interface ISpawnPointRegistry
    {
        IReadOnlyList<IPersistentSpawnPoint> All { get; }
        void Register(IPersistentSpawnPoint point);
        void Unregister(IPersistentSpawnPoint point);
    }
}
