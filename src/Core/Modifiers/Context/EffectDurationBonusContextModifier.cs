namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Enums;

    /// <summary>Extends the duration of effects the owner applies, filtered by status ("+1 bleed duration").
    /// The bonus is read lazily, so a source whose value changes (item upgrade) is picked up without re-attach.</summary>
    public class EffectDurationBonusContextModifier(StatusEffects statusFilter, Func<int> bonus)
        : ContextModifier(priority: Priority.Weak, id: "Context_Modifier_Effect_Duration_Bonus"), IEffectApplicationModifier
    {
        public EffectDurationBonusContextModifier(StatusEffects statusFilter, int bonus)
            : this(statusFilter, () => bonus)
        {
        }

        public void Apply(IEffectApplicationContext context)
        {
            if ((context.Effect.Status & statusFilter) == 0) return;
            context.Effect.Duration += bonus();
        }
    }
}
