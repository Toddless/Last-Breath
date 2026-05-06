namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Interfaces.Abilities;

    /// <summary>L3 upgrade: applies poison to all enemies on the battlefield.</summary>
    public class JoPUpgradeAllTargets(string id, string[] tags, int tier)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        private IJoPExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(JarOfPoison ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new JoPAllTargetsStrategy();
        }

        public override void RemoveUpgrade(JarOfPoison ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<JarOfPoison> Clone() =>
            new JoPUpgradeAllTargets(Id, Tags, Tier);
    }
}
