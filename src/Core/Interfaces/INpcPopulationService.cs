namespace Core.Interfaces
{
    /// <summary>Global cap of living NPCs on the map; spawn points reserve slots before spawning.</summary>
    public interface INpcPopulationService
    {
        int GlobalLimit { get; set; }
        int CurrentCount { get; }
        bool TryReserve();
    }
}
