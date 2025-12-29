namespace Battle.Source.Abilities.Effects
{
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class DamageOverTurnEffect(
        int duration,
        int maxStacks,
        float percentFromDamage = 0.7f,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Damage_Over_Turn", duration, maxStacks, statusEffect)
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
            if (Context.HasValue) Owner?.Effects.RegisterDotTick(new DotTick(DamagePerTick, Status, Id, Context.Value.Caster));
            base.TurnEnd();
        }

        public override bool IsStronger(IEffect otherEffect)
        {
            if (otherEffect is not DamageOverTurnEffect other) return false;

            return DamagePerTick > other.DamagePerTick;
        }

        public override IEffect Clone() => new DamageOverTurnEffect(Duration, MaxMaxStacks, PercentFromBase, Status);
    }
}
