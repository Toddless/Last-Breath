namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    /// <summary>
    /// Self-extending additional-attack-chance buff — the L3 "replace" variant of
    /// <see cref="CritCalculationBuff"/>: same stack/duration/extend-on-crit mechanic, but boosts
    /// <see cref="EntityParameter.AdditionalHitChance"/> instead of critical chance.
    /// </summary>
    public class AttackChanceCalculationBuff(int duration, int maxStacks, float value)
        : ParameterChangeEffect(
            id: "Effect_Attack_Chance_Calculation_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.AdditionalHitChance,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override void AfterAttack(IAttackContext context)
        {
            if (context.IsCritical) Duration++;
        }

        public override IEffect Copy() => new AttackChanceCalculationBuff(Duration, MaxStacks, Value);
    }
}
