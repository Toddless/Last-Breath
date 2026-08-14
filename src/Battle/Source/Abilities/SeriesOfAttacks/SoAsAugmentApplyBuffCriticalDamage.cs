namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Battle.Abilities;
    using Effects;

    public class SoAsAugmentApplyBuffCriticalDamage(string id, string[] tags, int tier, int amountAttack, float criticalDamage, int duration, int maxStacks)
        : AbilityAugment<SeriesOfAttacks>(id, tags, tier)
    {
        private readonly SoAsApplyBuffStrategy _buffStrategy = new(amountAttack, new CriticalDamageBuffEffect(duration, maxStacks, criticalDamage));

        private ISoAExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(SeriesOfAttacks ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = _buffStrategy;
        }

        public override void RemoveUpgrade(SeriesOfAttacks ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
        }

        public override IAbilityAugmentWrap<SeriesOfAttacks> Copy() => new SoAsAugmentApplyBuffCriticalDamage(Id, Tags, Tier, amountAttack, criticalDamage, duration, maxStacks);
    }
}
