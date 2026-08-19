namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// Base "Crit Calculation" buff: raises critical chance by <c>value</c> and extends its own
    /// duration by 1 turn each time the bearer scores a critical hit, within the instance's extension
    /// budget. (The "Lucky" mechanic is a separate effect — see <see cref="LuckyCritChanceEffect"/>.)
    /// </summary>
    public class CritCalculationBuff(int duration, int maxStacks, EffectValue value)
        : ParameterChangeEffect(
            id: "Effect_Crit_Calculation_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.CriticalChance,
            type: OperationType.Multiply,
            priority: Priority.Weak,
            statusEffect: StatusEffects.None,
            shape: EffectValueShape.ShareGained)
    {
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            // Self-extension travels the same road every other extender does, so a crit loop runs into
            // the instance's budget instead of holding the buff up for the rest of the fight.
            if (evt.Context.IsCritical) Extend(1);
        }

        public override IEffect Copy() => new CritCalculationBuff(Duration, MaxStacks, Authored);
    }
}
