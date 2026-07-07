namespace Battle.Source.Abilities.Modifiers
{
    using System;
    using Core.Battle;
    using Core.Enums;

    /// <summary>Pre-attack mutator: boosts the accuracy of the ability's attacks by a percentage.</summary>
    public class AccuracyAttackModifier(float amount) : IAttackModifier
    {
        public string Id => "Modifier_Accuracy_Attack";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Priority Priority { get; set; } = Priority.Weak;
        public bool IsSame(string otherId) => Id.Equals(otherId);

        public void Apply(IAttackContext context) => context.RawAccuracy *= 1 + amount;
    }
}
