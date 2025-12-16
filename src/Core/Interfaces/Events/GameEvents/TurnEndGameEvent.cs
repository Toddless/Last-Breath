namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;

    public record TurnEndGameEvent(IEntity CompletedTurn): IGameEvent, IBattleEvent, ICombatEvent;
}
