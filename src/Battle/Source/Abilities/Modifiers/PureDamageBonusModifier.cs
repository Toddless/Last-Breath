namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;

    /// <summary>
    /// Outgoing-damage mutator: adds <c>bonus</c> (0.5 = +50%) of the context's total as PURE damage.
    /// Applies only to damage DEALT by the owner (the handler also runs for incoming hits) and skips
    /// DoT ticks — they belong to their own effects, not to the cast being boosted.
    /// </summary>
    public class PureDamageBonusModifier(IFightable owner, float bonus) : IDamageModifier
    {
        public string Id => "Context_Modifier_Pure_Damage_Bonus";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Priority Priority { get; set; } = Priority.Weak;
        public bool IsSame(string otherId) => Id.Equals(otherId);

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
