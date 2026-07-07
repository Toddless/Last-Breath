namespace Battle.Source.Effects
{
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Enums;
    using Core.Events.GameEvents;

    public class HealingFuryEffect(
        int duration,
        int maxStacks,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(duration, maxStacks, healthPercent, statusEffect, id: "Effect_Healing_Fury")
    {
        private float _damageDealt;
        public float HealAmount { get; set; }

        protected override void OnAfterAttack(AfterAttackEvent evt)
        {
            if (evt.Context.Result is not AttackResults.Succeed) return;

            _damageDealt += evt.Context.FinalDamage;
        }

        public override void Remove()
        {
            float toHeal = _damageDealt * HealAmount;
            Target?.Heal(new HealContext(Target, Target){Amount = toHeal});
            base.Remove();
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not HealingFuryEffect healing) return false;

            return HealAmount > healing.HealAmount;
        }

        public override IEffect Copy() => new HealingFuryEffect(Duration, MaxStacks, HealthPercent, Status) { HealAmount = HealAmount };
    }
}
