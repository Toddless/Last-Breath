namespace Battle.Source.Abilities
{
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>Ability-scoped critical damage bonus (multicast abilities roll their own crits).</summary>
    public class AbilityUpgradeAdditionalCritDamage(string id, string[] tags, int tier, float amount)
        : SimpleUpgrade<Ability, AbilityParameter>(id, tags, tier, new SimpleAbilityParameterDecorator<AbilityParameter>(
            AbilityParameter.CriticalDamageBonus,
            Priority.Weak,
            OperationType.Add,
            amount,
            "Ability_Parameter_Decorator_Crit_Damage_Bonus",
            id));
}
