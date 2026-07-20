namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>Outgoing-damage mutator: converts <c>fraction</c> of the owner's PHYSICAL component into one
    /// element ("42% of physical damage becomes fire"). Absolute priority — the share is taken from the final
    /// number, after every numeric mutator. One element per instance by design: a pool offers the three as
    /// three entries and an item wears exactly one.</summary>
    public class ElementalConversionContextModifier(IFightable owner, DamageType element, Func<float> fraction, DamageCause? cause = null)
        : ContextModifier(priority: ContextModifierPriority.Absolute, id: "Context_Modifier_Elemental_Conversion"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (cause != null && context.Cause != cause) return;
            if (!context.DamageComponents.TryGetValue(DamageType.Physical, out float physical) || physical <= 0) return;

            context.Convert(DamageType.Physical, element, fraction());
        }
    }
}
