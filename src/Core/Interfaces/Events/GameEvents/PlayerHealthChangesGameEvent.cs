namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerHealthChangesGameEvent(IEntity Player, float Value) : IGameEvent, IBattleEvent;
}
