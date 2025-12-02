namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class HealingFuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(id, duration, healthPercent, statusEffect)
    {
        private float _damageDealt;
        public float HealAmount { get; set; }

        public override void OnAfterAttack(IEntity target, IAttackContext context)
        {
            if (!context.IsAttackSucceed) return;

            _damageDealt += context.FinalDamage;
        }

        public override void Remove(IEntity target)
        {
            float toHeal = _damageDealt * HealAmount;
            target.Heal(toHeal);
            base.Remove(target);
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not HealingFuryEffect healing) return false;

            return HealAmount > healing.HealAmount;
        }

        public override IEffect Clone() => new HealingFuryEffect(Id, Duration, HealthPercent, Status) { HealAmount = HealAmount };
    }
}
