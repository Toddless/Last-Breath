namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Interfaces.Abilities;

    /// <summary>L3 upgrade: remaining poison stacks transfer to another enemy when the target dies.</summary>
    public class JoPUpgradeTransferOnDeath(string id, string[] tags, int tier)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        private IJoPExecutionStrategy? _previousStrategy;

        public override void ApplyUpgrade(JarOfPoison ability)
        {
            _previousStrategy = ability.ExecutionStrategy;
            ability.ExecutionStrategy = new JoPTransferOnDeathStrategy();
        }

        public override void RemoveUpgrade(JarOfPoison ability)
        {
            if (_previousStrategy == null) return;
            ability.ExecutionStrategy = _previousStrategy;
            _previousStrategy = null;
        }

        public override IAbilityUpgradeWrap<JarOfPoison> Copy() =>
            new JoPUpgradeTransferOnDeath(Id, Tags, Tier);
    }
}
