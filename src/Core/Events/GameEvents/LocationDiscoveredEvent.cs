namespace Core.Events.GameEvents
{
    /// <summary>The player entered a marked location for the first time. Published by the
    /// world-side marker (which checks the facts registry to fire once per playthrough).</summary>
    public record LocationDiscoveredEvent(string LocationId) : IGameEvent;
}
