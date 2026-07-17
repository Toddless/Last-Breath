namespace Core.Modifiers.Context
{
    using Core.Context;
    using Entity;
    using Enums;

    /// <summary>
    /// Outgoing-damage mutator: adds <c>bonus</c> (0.5 = +50%) of the context's total as PURE damage.
    /// Applies only to damage DEALT by the owner (the handler also runs for incoming hits) and skips
    /// DoT ticks — they belong to their own effects, not to the cast being boosted.
    /// </summary>
    public class PureDamageBonusContextModifier(IFightable owner, float bonus)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Pure_Damage_Bonus"), IDamageModifier
    {
        /// <summary>Total damage this modifier boosted — the sacrifice heal upgrade reads it.</summary>
        public float DamageSeen { get; private set; }

        public void Apply(IDamageContext context)
        {
            if (!context.Source.IsSame(owner.InstanceId)) return;
            if (context.Cause is DamageCause.Effect) return;

            float bonusDamage = context.TotalDamage * bonus;
            if (bonusDamage <= 0) return;
            DamageSeen += context.TotalDamage;
            context.Add(DamageType.Pure, bonusDamage);
        }
    }
}
