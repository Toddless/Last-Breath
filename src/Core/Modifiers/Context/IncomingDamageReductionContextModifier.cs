namespace Core.Modifiers.Context
{
    using System.Linq;
    using Core.Context;
    using Enums;

    /// <summary>Reduces every damage component of incoming hits by <c>reduce</c> (0.25 = −25%).</summary>
    public class IncomingDamageReductionContextModifier(float reduce)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Incoming_Damage_Reduction"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * (1 - reduce));
        }
    }
}
