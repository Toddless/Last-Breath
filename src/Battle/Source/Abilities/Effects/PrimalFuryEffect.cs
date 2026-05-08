namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;

    public class PrimalFuryEffect(
        int duration,
        int maxMaxStacks,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury,
        string id = "Effect_Primal_Fury")
        : FuryEffect(duration, maxMaxStacks, healthPercent, statusEffect, id)
    {
        public float DamageMultiplier { get; set; }

        public override void BeforeAttack(IAttackContext context)
        {
            context.AdditionalDamage *= DamageMultiplier;
            base.BeforeAttack(context);
        }

        public override IEffect Copy() => new PrimalFuryEffect(Duration, MaxStacks, HealthPercent, Status, Id) { DamageMultiplier = DamageMultiplier };
    }
}
