namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;

    public class BurningFuryEffect(
        int duration,
        int maxStacks,
        EffectValue healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(duration, maxStacks, healthPercent, statusEffect, id: "Effect_Burning_Fury")
    {
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(BurnDamage)] = BurnShare * 100f;
                return values;
            }
        }

        /// <summary>Share of the health the fury burned off that the laid burn ticks for.</summary>
        public EffectValue BurnDamage { get; set; }

        public int BurningMaxStacks { get; set; }
        public int BurningDuration { get; set; }

        /// <summary>What the burn will actually tick with — the same reading the burn arrives at, kept
        /// here for the description and the strength comparison.</summary>
        public float BurnShare => Effective(BurnDamage);

        protected override void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Target == null) return;
            // The share goes over AUTHORED and the CAST's effectiveness travels with it: the burn
            // multiplies it once, on its own side, the way every laid-content-inside-laid-content
            // hand-off works here. What the fury grew to under the bearer's knobs is the fury's own —
            // the burn answers those knobs itself, as the damaging effect it is.
            var burnEffect = new DamageOverTurnEffect(
                BurningDuration,
                StatusEffects.Burning,
                BurningMaxStacks,
                BurnDamage);

            _ = burnEffect.Apply(new EffectApplyingContext
            {
                Target = evt.Context.Target,
                Caster = Target,
                // The burn feeds on the health the fury spent, not on a blow.
                Damage = DamageSnapshot.Of(DamageType.Fire, HealthBurned),
                Source = InstanceId,
                Effectiveness = CastEffectiveness,
                Trace = Trace,
                IsCritical = false
            });
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not BurningFuryEffect fury) return false;
            return BurnShare > fury.BurnShare;
        }

        public override IEffect Copy() => new BurningFuryEffect(Duration, MaxStacks, AuthoredHealthPercent, Status)
        {
            BurnDamage = BurnDamage, BurningMaxStacks = BurningMaxStacks, BurningDuration = BurningDuration
        };
    }
}
