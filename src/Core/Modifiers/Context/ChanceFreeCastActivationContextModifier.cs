namespace Core.Modifiers.Context
{
    using System;
    using Battle.Abilities;
    using Enums;

    /// <summary>Cast mutator: a chance that the activation costs nothing, whatever the cost type. Rolls on the
    /// context's own generator, so the mutator needs no source of randomness of its own.</summary>
    public class ChanceFreeCastActivationContextModifier(Func<float> chance)
        : ContextModifier(priority: ContextModifierPriority.Late, id: "Context_Modifier_Free_Cast_Chance"), IAbilityActivationModifier
    {
        public void Apply(IAbilityActivationContext context)
        {
            // Preview never rolls: availability stays pessimistic and the RNG stream is untouched.
            if (context.IsPreview || context.Rnd.RandFloat() > chance()) return;
            context.Cost = 0;
        }
    }
}
