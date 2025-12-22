namespace Battle.Source.Abilities.Effects
{
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class DamageOverTurnEffect(
        int duration,
        int stacks,
        float percentFromDamage = 0.7f,
        StatusEffects statusEffect = StatusEffects.None,
        string id = "Damage_Over_Turn_Effect")
        : Effect(id, duration, stacks, statusEffect)
    {
        public float PercentFromBase { get; } = percentFromDamage;
        public float DamagePerTick { get; set; }

        public override void Apply(EffectApplyingContext context)
        {
            DamagePerTick = context.Damage * PercentFromBase;
            base.Apply(context);
        }

        public override void TurnEnd()
        {
            if (Context.HasValue)
                Owner?.Effects.RegisterDotTick(new DotTick(DamagePerTick, Status, Context.Value.Caster));
            base.TurnEnd();
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not DamageOverTurnEffect other) return false;

            return DamagePerTick > other.DamagePerTick;
        }

        public override IEffect Clone() => new DamageOverTurnEffect(Duration, MaxStacks, PercentFromBase, Status, Id);
    }
}
