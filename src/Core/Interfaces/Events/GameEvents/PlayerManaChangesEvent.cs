namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerManaChangesEvent(IEntity Player, float Value) : IGameEvent, IBattleEvent;
}
