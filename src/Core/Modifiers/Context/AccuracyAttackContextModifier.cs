namespace Core.Modifiers.Context
{
    using Battle;
    using Enums;

    /// <summary>Pre-attack mutator: boosts the accuracy of the ability's attacks by a percentage.</summary>
    public class AccuracyAttackContextModifier(float amount)
        : ContextModifier(priority: Priority.Weak, id: "Modifier_Accuracy_Attack"), IAttackModifier
    {
        public void Apply(IAttackContext context) => context.RawAccuracy *= 1 + amount;
    }
}
