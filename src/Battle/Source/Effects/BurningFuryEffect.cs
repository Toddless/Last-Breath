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
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(duration, maxStacks, healthPercent, statusEffect, id: "Effect_Burning_Fury")
    {
        protected override Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = base.DescriptionValues;
                values[nameof(BurnDamage)] = BurnDamage * 100f;
                return values;
            }
        }

        public float BurnDamage { get; set; }
        public int BurningMaxStacks { get; set; }
        public int BurningDuration { get; set; }

        protected override void OnAfterAttack(AfterAttackEvent evt)
        {
            if (Target == null) return;
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
                IsCritical = false
            });
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not BurningFuryEffect fury) return false;
            return BurnDamage > fury.BurnDamage;
        }


        public override IEffect Copy() => new BurningFuryEffect(Duration, MaxStacks, HealthPercent, Status)
        {
            BurnDamage = BurnDamage, BurningMaxStacks = BurningMaxStacks, BurningDuration = BurningDuration
        };
    }
}
