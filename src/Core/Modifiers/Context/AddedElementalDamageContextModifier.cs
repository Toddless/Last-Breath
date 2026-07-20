namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Outgoing-damage mutator: ADDS a share of the hit's damage back as one element ("30% of attack
    /// damage is added as cold"). Late priority — after the numeric mutators that shape the base, but before
    /// the Absolute conversions, so the added element is itself convertible. The share is read from the total
    /// BEFORE the addition, so two such modifiers cannot feed each other.</summary>
    public class AddedElementalDamageContextModifier(IFightable owner, DamageType element, Func<float> fraction, DamageCause? cause = null)
        : ContextModifier(priority: ContextModifierPriority.Late, id: "Context_Modifier_Added_Elemental_Damage"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (cause != null && context.Cause != cause) return;

            float added = context.TotalDamage * fraction();
            if (added <= 0) return;

            context.Add(element, added);
        }
    }
}
