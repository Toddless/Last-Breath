namespace Battle.Source.Abilities.Effects
{
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;

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

        protected override string FormatDescription() => Localization.LocalizeDescriptionFormated(Id, HealthPercent, HealthAsDamageMultiplier);

        public override IEffect Clone() => new BurningFuryEffect(Duration, MaxMaxStacks, HealthPercent, Status)
        {
            HealthAsDamageMultiplier = HealthAsDamageMultiplier, BurningMaxStacks = BurningMaxStacks, BurningDuration = BurningDuration
        };
    }
}
