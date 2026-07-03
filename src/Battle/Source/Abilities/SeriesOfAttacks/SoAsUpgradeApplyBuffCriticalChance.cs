namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Interfaces.Abilities;
    using Effects;

    public class SoAsUpgradeApplyBuffCriticalChance(string id, string[] tags, int tier, int amountAttack, float criticalChance, int duration, int maxStacks)
        : AbilityUpgrade<SeriesOfDamagings>(id, tags, tier)
    {
        private readonly SoAsApplyBuffStrategy _buffStrategy = new(amountAttack, new CriticalChanceBuffEffect(duration, maxStacks, criticalChance));

        private ISoAExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(SeriesOfDamagings ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = _buffStrategy;
        }

        public override void RemoveUpgrade(SeriesOfDamagings ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
        }

        public override IAbilityUpgradeWrap<SeriesOfDamagings> Copy() => new SoAsUpgradeApplyBuffCriticalChance(Id, Tags, Tier, amountAttack, criticalChance, duration, maxStacks);
    }
}
