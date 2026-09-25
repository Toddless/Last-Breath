namespace Core.Modifiers.Context
{
    using System;
    using Battle.Abilities;
    using Core.Context;
    using Enums;

    /// <summary>Scales the per-tick damage of DoTs the owner applies, filtered by status ("+35% burning damage").
    /// The bonus is read lazily, so a source whose value changes (item upgrade) is picked up without re-attach.</summary>
    public class DotDamageBonusContextModifier(StatusEffects statusFilter, Func<float> bonus)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Dot_Damage_Bonus"), IEffectApplicationModifier
    {
        public DotDamageBonusContextModifier(StatusEffects statusFilter, float bonus)
            : this(statusFilter, () => bonus)
        {
        }

        public void Apply(IEffectApplicationContext context)
        {
            if (context.Effect is not IDamageOverTurnEffect dot) return;
            if ((dot.Status & statusFilter) == 0) return;
            dot.DamagePerTick *= 1 + bonus();
        }
    }
}
