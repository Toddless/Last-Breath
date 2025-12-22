namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    public class PrimalFuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(id, duration, healthPercent, statusEffect)
    {
        public float DamageMultiplier { get; set; }

        public override void BeforeAttack(IAttackContext context)
        {
            context.AdditionalDamage *= DamageMultiplier;
            base.BeforeAttack(context);
        }

        public override IEffect Clone() => new PrimalFuryEffect(Id, Duration, HealthPercent, Status) { DamageMultiplier = DamageMultiplier };
    }
}
