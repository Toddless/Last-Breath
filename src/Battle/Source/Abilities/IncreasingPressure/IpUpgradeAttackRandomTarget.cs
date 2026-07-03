namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Interfaces.Abilities;

    /// <summary>L3 upgrade: each attack also deals 45% of the damage to a random enemy on the battlefield.</summary>
    public class IpUpgradeAttackRandomTarget(string id, string[] tags, int tier, float splashPercent)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        private IIpExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(IncreasingPressure ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new IpDamageRandomTargetStrategy(splashPercent);
        }

        public override void RemoveUpgrade(IncreasingPressure ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeAttackRandomTarget(Id, Tags, Tier, splashPercent);
    }
}
