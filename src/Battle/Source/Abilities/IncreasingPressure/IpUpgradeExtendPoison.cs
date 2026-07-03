namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Interfaces.Abilities;

    /// <summary>L2 upgrade: each successful attack extends the poison duration on the target by 1 turn.</summary>
    public class IpUpgradeExtendPoison(string id, string[] tags, int tier, int extensionDuration)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        private IIpExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(IncreasingPressure ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new IpExtendPoisonDuration(extensionDuration);
        }

        public override void RemoveUpgrade(IncreasingPressure ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeExtendPoison(Id, Tags, Tier, extensionDuration);
    }
}
