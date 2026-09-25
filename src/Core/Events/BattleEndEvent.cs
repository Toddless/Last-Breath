namespace Core.Events
{
    using Battle;
    using Enums;

    // ICombatEvent: entities republish the battle end into their own combat bus so per-battle
    // state (once-per-battle passives) can reset without reaching for the battle bus.
    public record BattleEndEvent(BattleResults Results) : IGameEvent, IBattleEvent, ICombatEvent;
}
