namespace Battle.Source.Abilities
{
    using Core.Components.Decorator;
    using Core.Enums;

    public class AbilityUpgradeReduceCooldown(string id, string[] tags, int tier, float cooldown)
        : SimpleUpgrade<Ability, AbilityParameter>(id, tags, tier,
            new SimpleAbilityParameterDecorator<AbilityParameter>(
                AbilityParameter.Cooldown,
                Priority.Weak,
                OperationType.Subtract,
                cooldown,
                "",
                id));
}
