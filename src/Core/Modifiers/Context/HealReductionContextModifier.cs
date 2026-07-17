namespace Core.Modifiers.Context
{
    using Core.Context;
    using Enums;

    public class HealReductionContextModifier(float reduceBy)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Heal_Reduction"), IHealModifier
    {
        public void Apply(IHealContext context) => context.Amount *= 1 - reduceBy;
    }
}
