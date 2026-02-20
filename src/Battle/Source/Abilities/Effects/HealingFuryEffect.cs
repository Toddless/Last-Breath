namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    public class HealingFuryEffect(
        int duration,
        int maxStacks,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(duration, maxStacks, healthPercent, statusEffect, id: "Effect_Healing_Fury")
    {
        private float _damageDealt;
        public float HealAmount { get; set; }

        public override void AfterAttack(IAttackContext context)
        {
            if (context.Result is not AttackResults.Succeed) return;

            _damageDealt += context.FinalDamage;
        }

        public override void Remove()
        {
            float toHeal = _damageDealt * HealAmount;
            Owner?.Heal(toHeal);
            base.Remove();
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not HealingFuryEffect healing) return false;

            return HealAmount > healing.HealAmount;
        }

        public override IEffect Clone() => new HealingFuryEffect(Duration, MaxMaxStacks, HealthPercent, Status) { HealAmount = HealAmount };
    }
}
