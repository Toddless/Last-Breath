namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerHealthChangesEvent(IEntity Player, float Value) : IGameEvent, IBattleEvent;
}
