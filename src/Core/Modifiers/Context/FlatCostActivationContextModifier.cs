namespace Core.Modifiers.Context
{
    using Battle.Abilities;
    using Enums;

    /// <summary>Cast mutator: adds a flat <c>amount</c> to the activation cost (the Curse). Runs early
    /// so percentage mutators see the raised base.</summary>
    public class FlatCostActivationContextModifier(float amount)
        : ContextModifier(priority: ContextModifierPriority.Early, id: "Context_Modifier_Flat_Cost"), IAbilityActivationModifier
    {
        public void Apply(IAbilityActivationContext context) => context.Cost += amount;
    }
}
