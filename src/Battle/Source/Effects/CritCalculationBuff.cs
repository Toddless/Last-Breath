namespace Battle.Source.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;

    /// <summary>
    /// Base "Crit Calculation" buff: raises critical chance by <c>value</c> and extends its own
    /// duration by 1 turn each time the bearer scores a critical hit. (The "Lucky" mechanic is a
    /// separate L3 effect — see <see cref="LuckyCritChanceEffect"/>.)
    /// </summary>
    public class CritCalculationBuff(int duration, int maxStacks, float value)
        : ParameterChangeEffect(
            id: "Effect_Crit_Calculation_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.CriticalChance,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None)
    {
        public override void AfterAttack(IAttackContext context)
        {
            if (context.IsCritical) Duration++;
        }

        public override IEffect Copy() => new CritCalculationBuff(Duration, MaxStacks, Value);
    }
}
