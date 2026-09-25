namespace Core.Modifiers.Context
{
    using Battle.Abilities;
    using Enums;

    /// <summary>Cast mutator: every activation starts with <c>amount</c> extra cooldown (Seal of Slowness).</summary>
    public class CooldownIncreaseActivationContextModifier(float amount)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Cooldown_Increase"), IAbilityActivationModifier
    {
        public void Apply(IAbilityActivationContext context) => context.Cooldown += amount;
    }
}
