namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces;

    /// <summary>Reduces every damage component of incoming hits by <c>reduce</c> (0.25 = −25%).</summary>
    public class IncomingDamageReductionModifier(Priority priority, float reduce, string id = "Context_Modifier_Incoming_Damage_Reduction") : IDamageModifier
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public Priority Priority { get; set; } = priority;

        public void Apply(IDamageContext context)
        {
            foreach ((DamageType type, float damage) in context.DamageComponents.ToArray())
                context.Set(type, damage * (1 - reduce));
        }
    }
}
