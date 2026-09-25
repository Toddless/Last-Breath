namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Enums;

    /// <summary>Grants extra stacks of matching effects the owner applies ("+1 burning stack per application").
    /// The bonus is read lazily, so a source whose value changes (item upgrade) is picked up without re-attach.</summary>
    public class BonusEffectStacksContextModifier(StatusEffects statusFilter, Func<int> stacks)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Bonus_Effect_Stacks"), IEffectApplicationModifier
    {
        public BonusEffectStacksContextModifier(StatusEffects statusFilter, int stacks)
            : this(statusFilter, () => stacks)
        {
        }
        public void Apply(IEffectApplicationContext context)
        {
            if ((context.Effect.Status & statusFilter) == 0) return;
            context.BonusStacks += stacks();
        }
    }
}
