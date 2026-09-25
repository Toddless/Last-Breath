namespace Core.Modifiers.Context
{
    using System.Linq;
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>
    /// Scales every component of ability HITS dealt by <paramref name="owner"/> by <c>factor</c>
    /// (0.85 = −15%). Attacks and DoT ticks are untouched — this is the "hit damage" lever
    /// (Weakness debuff), distinct from the attack-damage parameter.
    /// </summary>
    public class HitDamageDealtContextModifier(IFightable owner, float factor)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Hit_Damage_Dealt"), IDamageModifier
    {
        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (context.Cause is not DamageCause.Ability) return;
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * factor);
        }
    }
}
