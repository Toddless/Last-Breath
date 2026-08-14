namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;

    /// <summary>
    /// L3 upgrade: replaces the series with one empowered strike that deals
    /// the total damage to the full series (with incremental scaling applied).
    /// </summary>
    public class IpAugmentSingleEmpoweredAttack(string id, string[] tags, int tier)
        : AbilityAugment<IncreasingPressure>(id, tags, tier)
    {
        private IIpExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(IncreasingPressure ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new IpSingleAttackExecutionStrategy();
        }

        public override void RemoveUpgrade(IncreasingPressure ability)
        {
            if (_previousStrategy != null)
                ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityAugmentWrap<IncreasingPressure> Copy() =>
            new IpAugmentSingleEmpoweredAttack(Id, Tags, Tier);
    }
}
