namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    /// <summary>
    /// A separate defense layer (NOT the barrier): absorbs post-mitigation damage until broken, then
    /// removes itself. Optional "while the shield holds" perk: percentage max-health regeneration at
    /// the bearer's turn end. Effectively permanent — the shield lives until shattered.
    /// </summary>
    public class ShieldEffect(EffectValue strength, EffectValue healthRegenPercent = default, int duration = 999)
        : Effect(id: "Effect_Shield", duration, maxStacks: 1), IShieldEffect
    {
        /// <summary>Damage this shield has already eaten. Held instead of a mutable strength so the
        /// authored figure stays authored and the effectiveness of the laying cast is read the one way
        /// every other figure is.</summary>
        private float _absorbed;

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(Strength)] = Strength;
                if (HealthRegenPercent != 0) values[nameof(healthRegenPercent)] = HealthRegenPercent * 100f;
                return values;
            }
        }

        public float Strength => MathF.Max(0f, Effective(strength) - _absorbed);

        /// <summary>Share of maximum health the bearer gets back each turn the shield holds.</summary>
        public float HealthRegenPercent => Effective(healthRegenPercent);

        public float Absorb(float damage)
        {
            float absorbed = Math.Min(Strength, damage);
            _absorbed += absorbed;
            if (Strength <= 0) Remove();
            return damage - absorbed;
        }

        public override void TurnEnd()
        {
            if (Target != null && HealthRegenPercent > 0)
                Target.Heal(new HealContext(Target, Target) { Amount = Target.Parameters.MaxHealth * HealthRegenPercent, Cause = RecoveryCause.Regen });
            base.TurnEnd();
        }

        // The AUTHORED figure plus what has already been eaten off it, so the copy is the shield as it
        // stands. Handing over the REMAINING strength would make it authored, and the copy would be
        // scaled by the effectiveness a second time.
        public override IEffect Copy() => new ShieldEffect(strength, healthRegenPercent, Duration) { _absorbed = _absorbed };
    }
}
