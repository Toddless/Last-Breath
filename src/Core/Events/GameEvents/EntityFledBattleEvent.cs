namespace Core.Events.GameEvents
{
    using Battle;
    using Entity;

    /// <summary>
    /// A fighter fled the battle alive: out of the fight, no death, reduced experience.
    /// Recorded to the timeline (a future beat/log hook) and published on the battle bus.
    /// </summary>
    public record EntityFledBattleEvent(IFightable Entity) : IGameEvent, IBattleEvent, ICombatEvent;
}
