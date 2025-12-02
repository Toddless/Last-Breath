namespace Battle.TestData.Abilities.Upgrades
{
    using Decorators;
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class ChangeAbilityCostTypeUpgrade(
        string id,
        string[] tags,
        Costs type,
        int tier,
        int pointPerRank,
        int maxRank,
        int currentRank = 0)
        : AbilityUpgrade(id, tags, tier, pointPerRank, maxRank, currentRank)
    {
        private AbilityParameterDecorator _upgradeDecorator = new AbilityParameterChangeCostTypeDecorator("Ability_Parameter_Change_Cost_Type_Decorator", type);
        public Costs CostType { get; } = type;

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

        public override IAbilityUpgrade Clone() => new ChangeAbilityCostTypeUpgrade(Id, Tags, CostType, Tier, PointPerRank, MaxRank, CurrentRank);
    }
}
