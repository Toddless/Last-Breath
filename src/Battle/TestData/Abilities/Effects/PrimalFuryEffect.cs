namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class PrimalFuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(id, duration, healthPercent, statusEffect)
    {
        public float DamageMultiplier { get; set; }

        public override void OnBeforeAttack(IEntity target, IAttackContext context)
        {
            context.FinalDamage *= DamageMultiplier;
            base.OnBeforeAttack(target, context);
        }

        public override IEffect Clone() => new PrimalFuryEffect(Id, Duration, HealthPercent, Status) { DamageMultiplier = DamageMultiplier };
    }
}
