namespace Battle.Source.Effects
{
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    public class BurningFuryEffect(
        int duration,
        int maxStacks,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(duration, maxStacks, healthPercent, statusEffect, id: "Effect_Burning_Fury")
    {
        public float HealthAsDamageMultiplier { get; set; }
        public int BurningMaxStacks { get; set; }
        public int BurningDuration { get; set; }

        protected override void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Target == null) return;
            var burnEffect = new DamageOverTurnEffect(
                BurningDuration,
                StatusEffects.Burning,
                BurningMaxStacks,
                HealthAsDamageMultiplier);

            burnEffect.Apply(new EffectApplyingContext
            {
                Target = evt.Context.Target,
                Caster = Target,
                Damage = HealthBurned,
                Source = InstanceId,
                IsCritical = false
            });
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not BurningFuryEffect fury) return false;
            return HealthAsDamageMultiplier > fury.HealthAsDamageMultiplier;
        }

        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values["BurnPercent"] = HealthAsDamageMultiplier * 100f;
                return values;
            }
        }

        public override IEffect Copy() => new BurningFuryEffect(Duration, MaxStacks, HealthPercent, Status)
        {
            HealthAsDamageMultiplier = HealthAsDamageMultiplier, BurningMaxStacks = BurningMaxStacks, BurningDuration = BurningDuration
        };
    }
}
