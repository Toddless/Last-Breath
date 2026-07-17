namespace Core.Entity.Components
{
    using Battle;
    using Battle.Abilities;
    using Context;

    /// <summary>
    /// Holds the entity-scoped pipeline mutators. Only context-mutating modifiers
    /// (<see cref="IContextModifier{T}"/> descendants) can be registered — reactions belong to CombatEvents.
    /// Modifiers are applied in <see cref="Core.Enums.Priority"/> order.
    /// </summary>
    public interface IModifierHandlerComponent
    {
        void Add(IAttackModifier modifier);
        void Add(IDamageModifier modifier);
        void Add(IHealModifier modifier);
        void Add(IManaRecoveryModifier modifier);
        void Add(IEffectApplicationModifier modifier);
        void Add(IIncomingEffectModifier modifier);
        void Add(IAbilityActivationModifier modifier);

        void Remove(IAttackModifier modifier);
        void Remove(IDamageModifier modifier);
        void Remove(IHealModifier modifier);
        void Remove(IManaRecoveryModifier modifier);
        void Remove(IEffectApplicationModifier modifier);
        void Remove(IAbilityActivationModifier modifier);

        void Apply(IAttackContext context);
        void Apply(IDamageContext context);
        void Apply(IHealContext context);
        void Apply(IManaRecoveryContext context);
        void Apply(IEffectApplicationContext context);
        void Apply(IIncomingEffectContext context);
        void Apply(IAbilityActivationContext context);
    }
}
