namespace Core.Interfaces.Events.GameEvents
{
    using Enums;
    using Battle;

    public record StatusEffectAppliedEvent(StatusEffects StatusEffect) : ICombatEvent
    {
    }
}
