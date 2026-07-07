namespace Core.Events.GameEvents
{
    using Entity;

    /// <summary>A highlighted spot was clicked during ability target selection (pick / toggle for Few).</summary>
    public record AbilityTargetPickedEvent(string SelectionId, IFightable Target) : IBattleEvent;
}
