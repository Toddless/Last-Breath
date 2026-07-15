namespace Battle.Source.Abilities
{
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>Ability-scoped critical chance bonus (multicast abilities roll their own crits).</summary>
    public class AbilityUpgradeAdditionalCritChance(string id, string[] tags, int tier, float amount)
        : SimpleUpgrade<Ability, AbilityParameter>(id, tags, tier, new SimpleAbilityParameterDecorator<AbilityParameter>(
            AbilityParameter.CriticalChanceBonus,
            Priority.Weak,
            OperationType.Add,
            amount,
            "Ability_Parameter_Decorator_Crit_Chance_Bonus",
            id));
}
