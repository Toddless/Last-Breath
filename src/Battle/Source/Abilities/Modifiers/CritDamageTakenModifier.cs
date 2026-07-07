namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using System.Linq;
    using Core.Context;
    using Core.Enums;

    /// <summary>Amplifies every damage component of incoming CRITICAL hits by <c>amp</c> (0.35 = +35%).</summary>
    public class CritDamageTakenModifier(Priority priority, float amp, string id = "Context_Modifier_Crit_Damage_Taken") : IDamageModifier
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public Priority Priority { get; set; } = priority;

        public void Apply(IDamageContext context)
        {
            if (!context.IsCrit) return;
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * (1 + amp));
        }
    }
}
