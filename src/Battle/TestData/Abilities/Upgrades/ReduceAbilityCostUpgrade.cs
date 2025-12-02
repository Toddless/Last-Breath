namespace Battle.TestData.Abilities.Upgrades
{
    using Decorators;
    using Core.Interfaces.Abilities;

    public class ReduceAbilityCostUpgrade : AbilityUpgrade
    {
        private readonly AbilityParameterDecorator _upgradeDecorator;
        public float CostValue { get; }

        public ReduceAbilityCostUpgrade(string id, string[] tags, int tier, int pointPerRank, int maxRank, float costValue)
            : base(id, tags, tier, pointPerRank, maxRank)
        {
            CostValue = costValue;
            _upgradeDecorator = new AbilityParameterChangeCostValueDecorator(() => CostValue * CurrentRank);
        }

        public override void ApplyUpgrade(IAbility ability)
        {
            ability.AddParameterUpgrade(_upgradeDecorator);
            base.ApplyUpgrade(ability);

        }
        
        public override void RemoveUpgrade(IAbility ability)
        {
            ability.RemoveParameterUpgrade(_upgradeDecorator.Id, _upgradeDecorator.Parameter);
            base.RemoveUpgrade(ability);
        }

        public override IAbilityUpgrade Clone() => new ReduceAbilityCostUpgrade(Id, Tags, Tier, PointPerRank, MaxRank, CostValue);
    }
}
