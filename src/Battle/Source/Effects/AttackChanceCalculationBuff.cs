namespace Battle.Source.Effects
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;

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
        public override async Task Apply(EffectApplyingContext context)
        {
            await base.Apply(context);
            if (Target == null) return;
            SubscribeUntilRemoved<AfterAttackEvent>(Target.CombatEvents, OnAfterAttack);
        }

        private void OnAfterAttack(AfterAttackEvent evt)
        {
            if (evt.Context.IsCritical) Duration++;
        }

        public override IEffect Copy() => new AttackChanceCalculationBuff(Duration, MaxStacks, Value);
    }
}
