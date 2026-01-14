namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;

    public record TurnStartEvent(IEntity StartedTurn) : IGameEvent, IBattleEvent, ICombatEvent;
}
