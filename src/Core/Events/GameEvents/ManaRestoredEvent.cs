namespace Core.Events.GameEvents
{
    using Battle;
    using Data;
    using Entity;

    /// <summary>Published after mana was actually gained (post-clamp). Amount is the effective gain.</summary>
    public record ManaRestoredEvent(IFightable Target, float Amount, VitalsSnapshot Vitals) : IBattleEvent, ICombatEvent;
}
