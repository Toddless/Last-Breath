namespace Core.Modifiers.Context
{
    using Battle.Abilities;
    using Enums;

    /// <summary>Cast mutator: the ability is paid with <paramref name="costType"/> instead of its own
    /// resource (Seal of Blood / Seal of Spirit). Categorical override — applied last.</summary>
    public class CostTypeActivationContextModifier(Costs costType)
        : ContextModifier(priority: ContextModifierPriority.Absolute, id: "Context_Modifier_Cost_Type_Override"), IAbilityActivationModifier
    {
        public void Apply(IAbilityActivationContext context) => context.CostType = costType;
    }
}
