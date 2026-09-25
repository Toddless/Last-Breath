namespace Core.Events
{
    using Battle;
    using Enums;

    public record StatusEffectAppliedEvent(StatusEffects StatusEffect) : ICombatEvent
    {
    }
}
