namespace Core.Modifiers.Context
{
    using System.Linq;
    using Core.Context;
    using Enums;

    /// <summary>Amplifies every damage component of incoming CRITICAL hits by <c>amp</c> (0.35 = +35%).</summary>
    public class CritDamageTakenContextModifier(float amp)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Crit_Damage_Taken"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.IsCrit) return;
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * (1 + amp));
        }
    }
}
