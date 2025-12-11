namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class PrimalFuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(id, duration, healthPercent, statusEffect)
    {
        public float DamageMultiplier { get; set; }

        public override void BeforeAttack(IEntity source, IAttackContext context)
        {
            context.FinalDamage *= DamageMultiplier;
            base.BeforeAttack(source, context);
        }

        public override IEffect Clone() => new PrimalFuryEffect(Id, Duration, HealthPercent, Status) { DamageMultiplier = DamageMultiplier };
    }
}
