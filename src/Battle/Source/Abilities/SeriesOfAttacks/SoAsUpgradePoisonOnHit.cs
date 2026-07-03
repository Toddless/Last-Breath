namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Interfaces.Abilities;

    public class SoAsUpgradePoisonOnHit(string id, string[] tags, int tier, int poisonDuration)
        : AbilityUpgrade<SeriesOfDamagings>(id, tags, tier)
    {
        private ISoAExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(SeriesOfDamagings ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new SoAsPoisonOnAttackExecutionStrategy(poisonDuration);
        }

        public override void RemoveUpgrade(SeriesOfDamagings ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<SeriesOfDamagings> Copy() => new SoAsUpgradePoisonOnHit(Id, Tags, Tier, poisonDuration);
    }
}
