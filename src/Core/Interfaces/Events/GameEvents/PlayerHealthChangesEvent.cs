namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerHealthChangesEvent(IFightable Player, float Value) : IGameEvent, IBattleEvent;
}
