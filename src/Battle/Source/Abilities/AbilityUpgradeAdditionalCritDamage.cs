namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Ability-scoped critical damage bonus (multicast abilities roll their own crits).</summary>
    public class AbilityUpgradeAdditionalCritDamage(string id, string[] tags, int tier, float amount)
        : SimpleUpgrade<Ability>(id, tags, tier, new SimpleAbilityParameterDecorator(
            AbilityParameter.CriticalDamageBonus,
            Priority.Weak,
            OperationType.Add,
            amount,
            $"Ability_Parameter_Decorator_{id}",
            id));
}
