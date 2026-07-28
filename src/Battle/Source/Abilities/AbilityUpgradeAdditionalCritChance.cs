namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Ability-scoped critical chance bonus (multicast abilities roll their own crits).</summary>
    public class AbilityUpgradeAdditionalCritChance(string id, string[] tags, int tier, float amount)
        : SimpleUpgrade<Ability>(id, tags, tier, new SimpleAbilityParameterDecorator(
            AbilityParameter.CriticalChanceBonus,
            Priority.Weak,
            OperationType.Add,
            amount,
            $"Ability_Parameter_Decorator_{id}",
            id));
}
