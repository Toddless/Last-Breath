namespace Core.Modifiers.Context
{
    using Battle.Abilities;
    using Enums;

    /// <summary>Cast mutator: the ability activates without a cooldown. Skips the excluded ability (its own source).</summary>
    public class NoCooldownActivationContextModifier(string excludedAbilityId)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_No_Cooldown_Cast"), IAbilityActivationModifier
    {
        public void Apply(IAbilityActivationContext context)
        {
            if (context.Ability.Id == excludedAbilityId) return;
            context.Cooldown = 0;
        }
    }
}
