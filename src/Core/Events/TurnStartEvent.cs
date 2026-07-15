namespace Core.Events
{
    using Battle;
    using Entity;

    public record TurnStartEvent(IFightable StartedTurn) : IGameEvent, IBattleEvent, ICombatEvent;
}
