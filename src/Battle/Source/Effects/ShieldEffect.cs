namespace Battle.Source.Effects
{
    using System;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;

    /// <summary>
    /// A separate defense layer (NOT the barrier): absorbs post-mitigation damage until broken, then
    /// removes itself. Optional "while the shield holds" perk: percentage max-health regeneration at
    /// the bearer's turn end. Effectively permanent — the shield lives until shattered.
    /// </summary>
    public class ShieldEffect(float strength, float healthRegenPercent = 0f, int duration = 999)
        : Effect(id: "Effect_Shield", duration, maxStacks: 1, StatusEffects.None), IShieldEffect
    {
        public float Strength { get; private set; } = strength;

        public float Absorb(float damage)
        {
            float absorbed = Math.Min(Strength, damage);
            Strength -= absorbed;
            if (Strength <= 0) Remove();
            return damage - absorbed;
        }

        public override void TurnEnd()
        {
            if (Target != null && healthRegenPercent > 0)
                Target.Heal(new HealContext(Target, Target)
                {
                    Amount = Target.Parameters.MaxHealth * healthRegenPercent,
                    Cause = HealCause.Regen
                });
            base.TurnEnd();
        }

        public override IEffect Copy() => new ShieldEffect(Strength, healthRegenPercent, Duration);

        protected override System.Collections.Generic.Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values["Strength"] = Strength;
                values["HealthRegenPercent"] = healthRegenPercent * 100f;
                return values;
            }
        }
    }
}
