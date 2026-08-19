namespace Battle.Source.Abilities.PoisonExplosion
{
    using Conditions;
    using Core.Battle.Abilities;

    public class PeAugmentExecuteBosses(string id, string[] tags, int tier, float stacksMultiplier = 1f)
        : Augment<PoisonExplosion>(id, tags, tier)
    {
        private IExecuteCondition? _previousCondition;

        public override void ApplyUpgrade(PoisonExplosion ability)
        {
            _previousCondition = ability.ExecuteCondition;
            ability.ExecuteCondition = new PoisonStacksBossCapableExecuteCondition(() => ability.ExecutionThreshold, stacksMultiplier);
        }
        public override void RemoveUpgrade(PoisonExplosion ability) => ability.ExecuteCondition = _previousCondition;

        public override IAugment Copy() => new PeAugmentExecuteBosses(Id, Tags, Tier, stacksMultiplier);
    }
}
