namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class BurningFuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(id, duration, healthPercent, statusEffect)
    {
        public float HealthAsDamageMultiplier { get; set; }
        public int BurningMaxStacks { get; set; }
        public int BurningDuration { get; set; }

        public override void OnAfterAttack(IEntity target, IAttackContext context)
        {
            var burnEffect = new DamageOverTurnEffect(
                BurningDuration,
                BurningMaxStacks,
                HealthAsDamageMultiplier,
                StatusEffects.Burning);

            burnEffect.OnApply(new EffectApplyingContext
            {
                Target = context.Target,
                Caster = target,
                Damage = HealthBurned,
                Source = Source,
                IsCritical = false
            });
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not BurningFuryEffect fury) return false;
            return HealthAsDamageMultiplier > fury.HealthAsDamageMultiplier;
        }

        public override IEffect Clone() => new BurningFuryEffect(Id, Duration, HealthPercent, Status)
        {
            HealthAsDamageMultiplier = HealthAsDamageMultiplier, BurningMaxStacks = BurningMaxStacks, BurningDuration = BurningDuration
        };
    }
}
