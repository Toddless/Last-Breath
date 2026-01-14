namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record PlayerMaxHealthChanges(IEntity Player, float Value) : IGameEvent, IBattleEvent;
}
