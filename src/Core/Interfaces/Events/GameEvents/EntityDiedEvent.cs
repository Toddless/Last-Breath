namespace Core.Interfaces.Events.GameEvents
{
    using Battle;
    using Entity;

    public record EntityDiedEvent(IFightable Entity) : IGameEvent, IBattleEvent, ICombatEvent;
}
