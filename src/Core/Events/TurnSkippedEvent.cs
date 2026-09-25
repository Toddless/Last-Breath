namespace Core.Events
{
    using Battle;
    using Entity;
    using Enums;

    /// <summary>
    /// The fighter's turn started and immediately ended because of a skip-turn status (Stun/Freeze).
    /// Published by the turn loop between OnTurnStart and OnTurnEnd, so in the timeline it sits
    /// between the start-of-turn effects and the end-of-turn effects (dots, cooldown ticks).
    /// </summary>
    public record TurnSkippedEvent(IFightable Fighter, StatusEffects Cause) : IBattleEvent, ICombatEvent;
}
