namespace Core.Services
{
    /// <summary>Global cap of living NPCs on the map; spawn points reserve slots before spawning.</summary>
    public interface INpcPopulationService
    {
        int GlobalLimit { get; set; }
        int CurrentCount { get; }
        bool TryReserve();

        /// <summary>Raid spawns: always succeeds and may push the count past the limit — regular
        /// spawn points simply pause until the world thins out. The final-death release is symmetric.</summary>
        void ReserveOutsideLimit();

        /// <summary>Save-load path: a scene reload frees NPC nodes without final-death events,
        /// so the counter must drop to zero before the fresh world starts reserving.</summary>
        void Reset();
    }
}
