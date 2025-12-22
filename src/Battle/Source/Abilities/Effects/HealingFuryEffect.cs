namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    public class HealingFuryEffect(
        string id,
        int duration,
        float healthPercent,
        StatusEffects statusEffect = StatusEffects.Fury)
        : FuryEffect(id, duration, healthPercent, statusEffect)
    {
        private float _damageDealt;
        public float HealAmount { get; set; }

        public override void AfterAttack( IAttackContext context)
        {
            if (!context.IsAttackSucceed) return;

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

        public override IEffect Clone() => new HealingFuryEffect(Id, Duration, HealthPercent, Status) { HealAmount = HealAmount };
    }
}
