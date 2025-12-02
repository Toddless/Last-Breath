namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class DamageOverTurnEffect(
        int duration,
        int stacks,
        float percentFromDamage = 0.7f,
        StatusEffects statusEffect = StatusEffects.None,
        string id = "Damage_Over_Turn_Effect")
        : Effect(id, duration, stacks, statusEffect)
    {
        public float PercentFromBase { get; set; } = percentFromDamage;
        public float DamagePerTick { get; set; }

        public override void OnApply(EffectApplyingContext context)
        {
            DamagePerTick = context.Damage * PercentFromBase;
            base.OnApply(context);
        }

        public override void OnTurnEnd(IEntity target)
        {
            target.TakeDamage(DamagePerTick, Status.GetDamageType(), DamageSource.Effect);
            base.OnTurnEnd(target);
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not DamageOverTurnEffect other) return false;

            return DamagePerTick > other.DamagePerTick;
        }

        public override IEffect Clone() => new DamageOverTurnEffect(Duration, MaxStacks, PercentFromBase, Status, Id);
    }
}
