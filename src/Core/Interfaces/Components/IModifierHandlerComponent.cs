namespace Core.Interfaces.Components
{
    using Battle;

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

        void Remove(IAttackModifier modifier);
        void Remove(IDamageModifier modifier);
        void Remove(IHealModifier modifier);

        void Apply(IAttackContext context);
        void Apply(IDamageContext context);
        void Apply(IHealContext context);
    }
}
