namespace Core.Interfaces.Events.GameEvents
{
    using Entity;

    public record AttackTargetSelectedGameEvent(IEntity Target) : IBattleEvent;
}
