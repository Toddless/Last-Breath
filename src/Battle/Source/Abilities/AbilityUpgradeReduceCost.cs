namespace Battle.Source.Abilities
{
    using Core.Enums;
    using Decorators;

    public class AbilityUpgradeReduceCost(string id, string[] tags, int tier, float cost)
        : SimpleUpgrade<Ability, AbilityParameter>(id, tags, tier,
            new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.CostValue,
                Priority.Weak,
                OperationType.Subtract,
                cost,
                "",
                id));
}
