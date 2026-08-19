namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    public class PrimalFuryEffect(
        int duration,
        int maxMaxStacks,
        EffectValue healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury,
        string id = "Effect_Primal_Fury")
        : FuryEffect(duration, maxMaxStacks, healthPercent, statusEffect, id)
    {
        /// <summary>Damage the attack GAINS, as the design list writes it (+35%); the factor the attack is
        /// scaled by is one plus it.</summary>
        public EffectValue DamageMultiplier { get; set; }

        public float DamageScale => Effective(DamageMultiplier, EffectValueShape.ShareGained);

        protected override void OnBeforeAttack(BeforeAttackEvent evt)
        {
            // Scales the whole attack. The old code multiplied only the ability-bonus part, which made
            // the buff a no-op on basic attacks.
            evt.Context.ScaleDamage(DamageScale);
            base.OnBeforeAttack(evt);
        }

        public override IEffect Copy() => new PrimalFuryEffect(Duration, MaxStacks, AuthoredHealthPercent, Status, Id) { DamageMultiplier = DamageMultiplier };
    }
}
