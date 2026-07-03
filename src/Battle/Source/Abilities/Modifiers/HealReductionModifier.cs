namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using Core.Enums;
    using Core.Interfaces;

    public class HealReductionModifier(Priority priority, float reduceBy, string id = "Context_Modifier_Heal_Reduction") : IHealModifier
    {
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => Id.Equals(otherId);
        public Priority Priority { get; set; } = priority;
        public void Apply(IHealContext context) => context.Amount *= 1 - reduceBy;
    }
}
