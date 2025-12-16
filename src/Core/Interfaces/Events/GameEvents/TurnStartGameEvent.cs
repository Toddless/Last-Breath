namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;

    public record TurnStartGameEvent(IEntity StartedTurn) : IGameEvent, IBattleEvent, ICombatEvent;
}
