namespace Core.Modifiers.Context
{
    using System;
    using Battle.Abilities;
    using Core.Context;
    using Enums;

    /// <summary>
    /// Scales how strongly what the owner applies LANDS — the entity-side twin of the cast's own
    /// effectiveness. It multiplies the figure the cast stamped rather than replacing it, so an augmented
    /// cast and an owner's knob compound; and because it belongs to whoever applies the effect rather than
    /// to a record, it also reaches effects no cast laid at all — a passive, an item grant, a boss stage.
    /// <para>Duration and stacks are separate axes and are never touched here, the same boundary the
    /// cast's own effectiveness is drawn at.</para>
    /// <para>No genus means every effect; a genus named means only its own kind, so a buff knob never
    /// reaches the debuffs the owner puts on somebody else. A kind knob and the unfiltered one are
    /// separate lines and both run, which is what makes them compound.</para>
    /// </summary>
    public class EffectEffectivenessContextModifier(EffectGenus? genus, Func<float> bonus)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Effect_Effectiveness"), IEffectApplicationModifier
    {
        public void Apply(IEffectApplicationContext context)
        {
            IEffect effect = context.Effect;
            if (genus is { } kind && effect.Genus != kind) return;

            float scale = 1 + bonus();
            effect.Effectiveness *= scale;

            // A damaging effect derives its tick from the blow BEFORE the pipeline runs (see
            // DamageOverTurnEffect.Apply), so the axis has to reach the number that has already been taken
            // off it. Everything else reads its figures through the effectiveness at the moment it is asked
            // and picks the new one up on its own.
            if (effect is IDamageOverTurnEffect dot) dot.DamagePerTick *= scale;
        }
    }
}
