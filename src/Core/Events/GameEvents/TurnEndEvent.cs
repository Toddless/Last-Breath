namespace Core.Events.GameEvents
{
    using Battle;

    public record TurnEndEvent: IGameEvent, IBattleEvent, ICombatEvent;
}
