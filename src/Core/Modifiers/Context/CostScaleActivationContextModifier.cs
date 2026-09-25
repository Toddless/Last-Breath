namespace Core.Modifiers.Context
{
    using Battle.Abilities;
    using Enums;

    /// <summary>Cast mutator: multiplies the activation cost by <c>factor</c>
    /// (1.25 = +25% Clouded Mind, 0.75 = −25% Sorcery Gift).</summary>
    public class CostScaleActivationContextModifier(float factor)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Cost_Scale"), IAbilityActivationModifier
    {
        public void Apply(IAbilityActivationContext context) => context.Cost *= factor;
    }
}
