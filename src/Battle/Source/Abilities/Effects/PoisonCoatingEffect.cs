namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// Buff applied to the caster. Each attack made while this effect is active applies a poison stack to the target.
    /// </summary>
    public class PoisonCoatingEffect(
        int duration,
        int maxStacks,
        int poisonDuration,
        float poisonDamagePercent = 0.7f,
        StatusEffects statusEffect = StatusEffects.None)
        : Effect(id: "Effect_Poison_Coating", duration, maxStacks, statusEffect)
    {
        public int PoisonDuration { get; } = poisonDuration;
        public float PoisonDamagePercent { get; } = poisonDamagePercent;

        public override void AfterAttack(IAttackContext context)
        {
            if (Target == null) return;
            if (context.Result != AttackResults.Succeed) return;

            var poison = new DamageOverTurnEffect(PoisonDuration, 999, StatusEffects.Poison, PoisonDamagePercent);
            var applyContext = new EffectApplyingContext
            {
                Caster = Target,
                Target = context.Target,
                Source = Id,
                Damage = context.FinalDamage,
                IsCritical = context.IsCritical
            };
            poison.Apply(applyContext);
        }

        public override IEffect Copy() =>
            new PoisonCoatingEffect(Duration, MaxStacks, PoisonDuration, PoisonDamagePercent, Status);
    }
}
