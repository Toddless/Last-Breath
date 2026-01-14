namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    public class PrimalFuryEffect(
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury,
        string id = "Effect_Primal_Fury")
        : FuryEffect(duration, healthPercent, statusEffect, id)
    {
        public float DamageMultiplier { get; set; }

        public override void BeforeAttack(IAttackContext context)
        {
            context.AdditionalDamage *= DamageMultiplier;
            base.BeforeAttack(context);
        }

        public override IEffect Clone() => new PrimalFuryEffect(Duration, HealthPercent, Status, Id) { DamageMultiplier = DamageMultiplier };
    }
}
