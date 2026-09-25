namespace Core.Modifiers.Context
{
    using System;
    using System.Runtime.CompilerServices;
    using Battle.Abilities;
    using Core.Context;
    using Enums;
    using Godot;

    /// <summary>Scales the duration of EVERY effect the owner applies ("doubles the duration of applied
    /// effects"). Deliberately unfiltered — unlike <see cref="EffectDurationBonusContextModifier"/>, which adds
    /// whole turns to one status.
    /// <para>Duration is whole turns, but the owner's knobs run over the same effect one after another, and
    /// rounding each step on its own threw the fraction away every time: twenty "+10%" lines left a 4-turn
    /// effect at 4 turns, while the same lines on a 10-turn effect compounded normally. The dropped fraction
    /// is therefore carried alongside the effect instance and picked up by the next line, so the chain lands
    /// where a single combined line would. The carry is rebased on the effect's CURRENT duration each time,
    /// which keeps whole-turn knobs running in between honest; entries die with the effect instance.</para></summary>
    public class EffectDurationScaleContextModifier(Func<float> bonus)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Effect_Duration_Scale"), IEffectApplicationModifier
    {
        // Shared by every instance of the knob on purpose: the fraction one line drops belongs to the effect,
        // not to the line, and the next line over the same effect must find it.
        private static readonly ConditionalWeakTable<IEffect, StrongBox<float>> s_carriedFractions = new();

        public void Apply(IEffectApplicationContext context)
        {
            IEffect effect = context.Effect;
            StrongBox<float> carried = s_carriedFractions.GetValue(effect, _ => new StrongBox<float>(0f));

            float scaled = (effect.Duration + carried.Value) * (1 + bonus());
            int turns = Mathf.RoundToInt(scaled);

            carried.Value = scaled - turns;
            effect.Duration = turns;
        }
    }
}
