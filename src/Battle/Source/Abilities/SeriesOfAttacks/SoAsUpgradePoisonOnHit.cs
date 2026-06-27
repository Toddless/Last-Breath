namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Interfaces.Abilities;

    public class SoAsUpgradePoisonOnHit(string id, string[] tags, int tier, int poisonDuration)
        : AbilityUpgrade<SeriesOfAttacks>(id, tags, tier)
    {
        private ISoAExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(SeriesOfAttacks ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new SoAsPoisonOnAttackExecutionStrategy(poisonDuration);
        }

        public override void RemoveUpgrade(SeriesOfAttacks ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<SeriesOfAttacks> Copy() => new SoAsUpgradePoisonOnHit(Id, Tags, Tier, poisonDuration);
    }
}
