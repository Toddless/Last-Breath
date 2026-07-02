namespace Battle.Source.Abilities.PoisonExplosion
{
    using Conditions;
    using Core.Interfaces.Abilities;

    public class PeUpgradeExecuteBosses(string id, string[] tags, int tier, float stacksMultiplier = 1f)
        : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        private IExecuteCondition? _previousCondition;

        public override void ApplyUpgrade(PoisonExplosion ability)
        {
            _previousCondition = ability.ExecuteCondition;
            ability.ExecuteCondition = new PoisonStacksBossCapableExecuteCondition(() => ability.ExecutionThreshold, stacksMultiplier);
        }
        public override void RemoveUpgrade(PoisonExplosion ability) => ability.ExecuteCondition = _previousCondition;

        public override IAbilityUpgrade Copy() => new PeUpgradeExecuteBosses(Id, Tags, Tier, stacksMultiplier);
    }
}
