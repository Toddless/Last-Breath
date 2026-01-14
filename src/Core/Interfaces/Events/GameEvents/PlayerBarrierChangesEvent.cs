namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerBarrierChangesEvent(IEntity Player, float Value) : IGameEvent, IBattleEvent;
}
