namespace Core.Events.GameEvents
{
    using Enums;
    using Godot;

    /// <summary>
    /// A reputation-relevant deed by the player (helping, donating, stealing…). Any system may
    /// publish it on the game bus; the ReputationDeedProcessor resolves the deltas from
    /// ReputationDeeds.json. Kills are NOT published this way — the processor derives them
    /// from EntityDiedEvent itself. Position anchors the future witness lookup.
    /// </summary>
    public record PlayerDeedEvent(string DeedId, Fractions TargetFaction, string? TargetInstanceId, Vector2 Position) : IGameEvent;
}
