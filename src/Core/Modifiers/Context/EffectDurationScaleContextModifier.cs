namespace Core.Modifiers.Context
{
    using System;
    using Core.Context;
    using Enums;
    using Godot;

    /// <summary>Scales the duration of EVERY effect the owner applies ("doubles the duration of applied
    /// effects"). Deliberately unfiltered — unlike <see cref="EffectDurationBonusContextModifier"/>, which adds
    /// whole turns to one status. Duration is whole turns, so the scaled value rounds.</summary>
    public class EffectDurationScaleContextModifier(Func<float> bonus)
        : ContextModifier(priority: ContextModifierPriority.Normal, id: "Context_Modifier_Effect_Duration_Scale"), IEffectApplicationModifier
    {
        public void Apply(IEffectApplicationContext context) =>
            context.Effect.Duration = Mathf.RoundToInt(context.Effect.Duration * (1 + bonus()));
    }
}
