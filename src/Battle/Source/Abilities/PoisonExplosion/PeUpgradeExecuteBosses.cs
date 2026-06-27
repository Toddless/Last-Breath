namespace Battle.Source.Abilities.PoisonExplosion
{
    using Conditions;
    using Core.Interfaces.Abilities;

    public class PeUpgradeExecuteBosses(string id, string[] tags, int tier) : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        private IExecuteCondition? _previousCondition;

        public override void ApplyUpgrade(PoisonExplosion ability)
        {
            _previousCondition = ability.ExecuteCondition;
            ability.ExecuteCondition = new PoisonStacksBossCapableExecuteCondition(() => ability.ExecutionThreshold);
        }

        public override IAbilityUpgrade Copy() => new PeUpgradeExecuteBosses(Id, Tags, Tier);

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.ExecuteCondition = _previousCondition;
    }
}
