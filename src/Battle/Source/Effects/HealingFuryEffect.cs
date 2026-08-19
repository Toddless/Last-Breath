namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events;

    public class HealingFuryEffect(
        int duration,
        int maxStacks,
        EffectValue healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(duration, maxStacks, healthPercent, statusEffect, id: "Effect_Healing_Fury")
    {
        private float _damageDealt;

        /// <summary>Share of the damage dealt under the fury that comes back as health when it ends.</summary>
        public EffectValue HealAmount { get; set; }

        public float HealShare => Effective(HealAmount);

        protected override void OnAfterAttack(AfterAttackEvent evt)
        {
            if (evt.Context.Result is not AttackResults.Succeed) return;

            _damageDealt += evt.Context.FinalDamage.Total;
        }

        public override void Remove()
        {
            float toHeal = _damageDealt * HealShare;
            Target?.Heal(new HealContext(Target, Target) { Amount = toHeal });
            base.Remove();
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not HealingFuryEffect healing) return false;

            return HealShare > healing.HealShare;
        }

        public override IEffect Copy() => new HealingFuryEffect(Duration, MaxStacks, AuthoredHealthPercent, Status) { HealAmount = HealAmount };
    }
}
