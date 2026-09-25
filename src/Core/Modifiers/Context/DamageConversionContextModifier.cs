namespace Core.Modifiers.Context
{
    using System;
    using System.Linq;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>
    /// Outgoing-damage mutator: converts <c>fraction</c> of every non-Sacred component of the owner's
    /// damage into Sacred. Conversion runs at Absolute priority — after every numeric mutator.
    /// The fraction is read lazily, so a source whose value changes (item upgrade) is picked up
    /// without re-attach; the plain-float twin serves the fixed sources (effects, upgrades).
    /// </summary>
    public class DamageConversionContextModifier(IFightable owner, Func<float> fraction, DamageCause cause)
        : ContextModifier(priority: ContextModifierPriority.Absolute, id: "Context_Modifier_Sacred_Conversion"), IDamageModifier
    {
        public DamageConversionContextModifier(IFightable owner, float fraction, DamageCause cause)
            : this(owner, () => fraction, cause)
        {
        }

        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (context.Cause != cause) return;

            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
            {
                if (type is DamageType.Sacred || damage <= 0) continue;
                context.Convert(type, DamageType.Sacred, fraction());
            }
        }
    }
}
