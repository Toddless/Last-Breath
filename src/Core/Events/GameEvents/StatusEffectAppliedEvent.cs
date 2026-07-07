namespace Core.Events.GameEvents
{
    using Battle;
    using Enums;

    public record StatusEffectAppliedEvent(StatusEffects StatusEffect) : ICombatEvent
    {
    }
}
