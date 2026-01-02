namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;

    public record TurnEndEvent(IEntity CompletedTurn): IGameEvent, IBattleEvent, ICombatEvent;
}
