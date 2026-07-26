namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    public class PrimalFuryEffect(
        int duration,
        int maxMaxStacks,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury,
        string id = "Effect_Primal_Fury")
        : FuryEffect(duration, maxMaxStacks, healthPercent, statusEffect, id)
    {
        public float DamageMultiplier { get; set; }

        protected override void OnBeforeAttack(BeforeAttackEvent evt)
        {
            // Scales the whole attack (×{damageMultiplier} per the upgrade description). The old code
            // multiplied only the ability-bonus part, which made the buff a no-op on basic attacks.
            evt.Context.ScaleDamage(DamageMultiplier);
            base.OnBeforeAttack(evt);
        }

        public override IEffect Copy() => new PrimalFuryEffect(Duration, MaxStacks, HealthPercent, Status, Id) { DamageMultiplier = DamageMultiplier };
    }
}
