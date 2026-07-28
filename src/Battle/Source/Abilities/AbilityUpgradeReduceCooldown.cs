namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class AbilityUpgradeReduceCooldown(string id, string[] tags, int tier, float cooldown)
        : SimpleUpgrade<Ability>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                AbilityParameter.Cooldown,
                Priority.Weak,
                OperationType.Subtract,
                cooldown,
                $"Ability_Parameter_Decorator_{id}",
                id));
}
