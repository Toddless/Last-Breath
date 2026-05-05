namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Linq;
    using Core.Interfaces.Abilities;

    /// <summary>
    /// L3 upgrade: replaces the series with one empowered strike that deals
    /// the total damage to the full series (with incremental scaling applied).
    /// </summary>
    public class IpUpgradeSingleEmpoweredStrike(string id, string[] tags, int tier)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        private IIpExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(IncreasingPressure ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new IpSingleAttackExecutionStrategy(_previousStrategy.Modifiers.ToList());
        }

        public override void RemoveUpgrade(IncreasingPressure ability)
        {
            if (_previousStrategy != null)
                ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<IncreasingPressure> Clone() =>
            new IpUpgradeSingleEmpoweredStrike(Id, Tags, Tier);
    }
}
