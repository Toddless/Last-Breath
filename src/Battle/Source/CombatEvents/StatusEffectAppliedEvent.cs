namespace Battle.Source.CombatEvents
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public record StatusEffectAppliedEvent(IEntity Source, StatusEffects StatusEffect) : ICombatEvent
    {

    }
}
