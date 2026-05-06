namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Interfaces.Abilities;

    /// <summary>L3 upgrade: the jar bounces 5 times between random enemies, applying poison each time.</summary>
    public class JoPUpgradeBouncing(string id, string[] tags, int tier, int bounces = 5)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        private IJoPExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(JarOfPoison ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new JoPBouncingStrategy(bounces);
        }

        public override void RemoveUpgrade(JarOfPoison ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<JarOfPoison> Clone() =>
            new JoPUpgradeBouncing(Id, Tags, Tier, bounces);
    }
}
