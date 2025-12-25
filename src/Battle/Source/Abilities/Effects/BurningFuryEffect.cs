namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;

    public class BurningFuryEffect(
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(id: "Effect_Burning_Fury", duration, healthPercent, statusEffect)
    {
        public float HealthAsDamageMultiplier { get; set; }
        public int BurningMaxStacks { get; set; }
        public int BurningDuration { get; set; }

        public override void AfterAttack(IAttackContext context)
        {
            if (Owner == null) return;
            var burnEffect = new DamageOverTurnEffect(
                BurningDuration,
                BurningMaxStacks,
                HealthAsDamageMultiplier,
                StatusEffects.Burning);

            burnEffect.Apply(new EffectApplyingContext
            {
                Target = context.Target,
                Caster = Owner,
                Damage = HealthBurned,
                Source = Id,
                IsCritical = false
            });
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not BurningFuryEffect fury) return false;
            return HealthAsDamageMultiplier > fury.HealthAsDamageMultiplier;
        }

        public override IEffect Clone() => new BurningFuryEffect( Duration, HealthPercent, Status)
        {
            HealthAsDamageMultiplier = HealthAsDamageMultiplier, BurningMaxStacks = BurningMaxStacks, BurningDuration = BurningDuration
        };
    }
}
