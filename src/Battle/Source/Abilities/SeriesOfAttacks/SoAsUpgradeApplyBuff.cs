namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Interfaces.Abilities;

    public class SoAsUpgradeApplyBuff(string id, string[] tags, int tier, int amountAttack, IEffect toApply)
        : AbilityUpgrade<SeriesOfAttacks>(id, tags, tier)
    {
        private readonly SoAsApplyBuffStrategy _buffStrategy = new (amountAttack, toApply);
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


        public override IAbilityUpgradeWrap<SeriesOfAttacks> Clone() => new SoAsUpgradeApplyBuff(Id, Tags, Tier, amountAttack, toApply);
    }
}
