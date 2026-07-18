namespace Core.Modifiers.Context
{
    using System.Linq;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>
    /// Outgoing-damage mutator: converts <c>fraction</c> of every non-Pure component of the owner's
    /// ability damage into Pure. Conversion runs at Absolute priority — after every numeric mutator.
    /// </summary>
    public class PureConversionContextModifier(IFightable owner, float fraction)
        : ContextModifier(priority: ContextModifierPriority.Absolute, id: "Context_Modifier_Pure_Conversion"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (context.Cause is not DamageCause.Ability) return;

            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
            {
                if (type is DamageType.Pure || damage <= 0) continue;
                context.Convert(type, DamageType.Pure, fraction);
            }
        }
    }
}
