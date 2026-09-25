namespace Core.Events
{
    using Battle;

    public record TurnEndEvent: IGameEvent, IBattleEvent, ICombatEvent;
}
