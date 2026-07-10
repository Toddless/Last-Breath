namespace Core.Events.GameEvents
{
    using Entity;
    using Enums;

    /// <summary>Game-bus mirror of IFactionRelationService.PlayerReputationChanged — interested
    /// systems subscribe here instead of injecting the service.</summary>
    public record ReputationChangedEvent(ReputationChangedArgs Change) : IGameEvent;

    /// <summary>Game-bus mirror of IFactionRelationService.PlayerRelationChanged (threshold crossings only).</summary>
    public record PlayerStandingChangedEvent(Fractions Faction, RelationLevel Level) : IGameEvent;
}
