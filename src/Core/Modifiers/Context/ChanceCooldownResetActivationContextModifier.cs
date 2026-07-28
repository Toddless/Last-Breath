namespace Core.Modifiers.Context
{
    using System;
    using Battle.Abilities;
    using Enums;

    /// <summary>Cast mutator: a chance that the ability comes off cooldown immediately. Late priority — it
    /// overrides whatever the numeric cooldown mutators settled on.</summary>
    public class ChanceCooldownResetActivationContextModifier(Func<float> chance)
        : ContextModifier(priority: ContextModifierPriority.Late, id: "Context_Modifier_Cooldown_Reset_Chance"), IAbilityActivationModifier
    {
        public void Apply(IAbilityActivationContext context)
        {
            // Preview never rolls: availability stays pessimistic and the RNG stream is untouched.
            if (context.IsPreview || context.Rnd.Randf() > chance()) return;
            context.Cooldown = 0;
        }
    }
}
