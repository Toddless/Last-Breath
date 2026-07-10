namespace Core.Events.GameEvents
{
    /// <summary>The player ends the turn without attacking (HUD button). The arena resolves the
    /// pending target selection with no target — effects and cooldowns tick as usual.</summary>
    public record PlayerEndTurnRequestedEvent : IBattleEvent;
}
