namespace Core.Modifiers.Context
{
    using System;
    using Battle;
    using Core.Context;
    using Enums;

    /// <summary>
    /// Diminishing returns of hard control on its bearer: application N of a hard-CC status keeps
    /// duration × multipliers[N] (ceil, so a shortened stun still lasts at least a turn); applications
    /// past the list are resisted outright. Resistance FADES: full resistance decays back to zero over
    /// <see cref="ControlResistanceRules.ResistanceDecayTurns"/> of the bearer's turns (DecayTick).
    /// </summary>
    public class ControlResistanceModifier(ControlResistanceRules rules)
        : ContextModifier(ContextModifierPriority.Late, id: "Context_Modifier_Control_Resistance"), IIncomingEffectModifier
    {
        /// <summary>Accumulated resistance in "applications"; the floor is the current tier.</summary>
        private float _resistance;

        public void Apply(IIncomingEffectContext context)
        {
            if ((context.Effect.Status & rules.HardControlMask) == 0) return;
            int tier = (int)_resistance;
            if (tier >= rules.DurationMultipliers.Count)
            {
                context.Rejected = true;
                return;
            }

            context.Effect.Duration = (int)Math.Ceiling(context.Effect.Duration * rules.DurationMultipliers[tier]);
            // A fresh application snaps to the next full tier — partial decay does not soften new CC.
            _resistance = Math.Min(tier + 1, rules.DurationMultipliers.Count);
        }

        /// <summary>Once per bearer's turn: full resistance fades to zero over the configured turns.</summary>
        public void DecayTick() =>
            _resistance = Math.Max(0f, _resistance - (float)rules.DurationMultipliers.Count / Math.Max(1, rules.ResistanceDecayTurns));
    }
}
