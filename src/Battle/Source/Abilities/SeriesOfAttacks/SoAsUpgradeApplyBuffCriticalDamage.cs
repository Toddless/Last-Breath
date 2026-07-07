namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Battle.Abilities;
    using Effects;

    public class SoAsUpgradeApplyBuffCriticalDamage(string id, string[] tags, int tier, int amountAttack, float criticalDamage, int duration, int maxStacks)
        : AbilityUpgrade<SeriesOfAttacks>(id, tags, tier)
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

        public override IAbilityUpgradeWrap<SeriesOfAttacks> Copy() => new SoAsUpgradeApplyBuffCriticalDamage(Id, Tags, Tier, amountAttack, criticalDamage, duration, maxStacks);
    }
}
