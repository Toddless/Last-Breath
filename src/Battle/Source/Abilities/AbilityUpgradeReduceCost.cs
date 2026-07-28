namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class AbilityUpgradeReduceCost(string id, string[] tags, int tier, float cost)
        : SimpleUpgrade<Ability>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                AbilityParameter.CostValue,
                Priority.Weak,
                OperationType.Subtract,
                cost,
                $"Ability_Parameter_Decorator_{id}",
                id));
}
