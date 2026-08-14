namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Enums;
    using Core.Events;

    /// <summary>
    /// Self-extending additional-attack-chance buff — the L3 "replace" variant of
    /// <see cref="CritCalculationBuff"/>: same stack/duration/extend-on-crit mechanic (budget and
    /// all), but boosts
    /// <see cref="EntityParameter.AdditionalHitChance"/> instead of critical chance.
    /// </summary>
    public class AttackChanceCalculationBuff(int duration, int maxStacks, EffectValue value)
        : ParameterChangeEffect(
            id: "Effect_Attack_Chance_Calculation_Buff",
            duration,
            maxStacks,
            value,
            parameter: EntityParameter.AdditionalHitChance,
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

        public override IEffect Copy() => new AttackChanceCalculationBuff(Duration, MaxStacks, Authored);
    }
}
