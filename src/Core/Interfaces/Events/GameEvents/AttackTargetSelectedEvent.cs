namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record AttackTargetSelectedEvent(IEntity Target) : IBattleEvent;
}
