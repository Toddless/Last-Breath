namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Enums;

    /// <summary>Scales incoming heals up ("healing efficiency +X%"). Counterpart of HealReductionModifier.
    /// The bonus is read lazily, so a source whose value changes (item upgrade) is picked up without re-attach.</summary>
    public class HealingBonusContextModifier(Func<float> bonus)
        : ContextModifier(priority: Priority.Weak, id: "Context_Modifier_Healing_Bonus"), IHealModifier
    {
        public HealingBonusContextModifier(float bonus)
            : this(() => bonus)
        {
        }

        public void Apply(IHealContext context) => context.Amount *= 1 + bonus();
    }
}
