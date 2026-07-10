namespace Core.Events.GameEvents
{
    using Battle;
    using Entity;

    /// <summary>Killer = the source of the lethal hit; null when death had no attacker
    /// (world defeat in a skirmish, lifecycle, debug kill).</summary>
    public record EntityDiedEvent(IFightable Entity, IFightable? Killer = null) : IGameEvent, IBattleEvent, ICombatEvent;
}
