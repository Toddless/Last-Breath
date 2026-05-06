namespace Battle.Source.Abilities.IncreasingPressure
{
    using System.Linq;
    using Core.Interfaces.Abilities;

    /// <summary>L3 upgrade: each attack also deals 45% of the damage to a random enemy on the battlefield.</summary>
    public class IpUpgradeDamageRandomTarget(string id, string[] tags, int tier, float splashPercent = 0.45f)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        private IIpExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(IncreasingPressure ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new IpDamageRandomTargetStrategy(splashPercent, _previousStrategy.Modifiers.ToList());
        }

        public override void RemoveUpgrade(IncreasingPressure ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<IncreasingPressure> Clone() =>
            new IpUpgradeDamageRandomTarget(Id, Tags, Tier, splashPercent);
    }
}
