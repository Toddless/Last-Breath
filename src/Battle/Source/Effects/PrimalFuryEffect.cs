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
            evt.Context.AdditionalDamage *= DamageMultiplier;
            base.OnBeforeAttack(evt);
        }

        public override IEffect Copy() => new PrimalFuryEffect(Duration, MaxStacks, HealthPercent, Status, Id) { DamageMultiplier = DamageMultiplier };
    }
}
