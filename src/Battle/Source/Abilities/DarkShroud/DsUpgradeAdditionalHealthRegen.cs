namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Enums;
    using Decorators;

    /// <summary>
    /// L2 upgrade: increases the percentage of max health restored each turn by the shroud.
    /// </summary>
    public class DsUpgradeAdditionalHealthRegen(string id, string[] tags, int tier, float additionalRegen)
        : SimpleUpgrade<DarkShroud, DarkShroud.Parameters>(id, tags, tier,
            new SimpleAbilityParameterDecorator<DarkShroud.Parameters>(
                DarkShroud.Parameters.HealthRegen,
                Priority.Weak,
                OperationType.Add,
                additionalRegen,
                "Ability_Parameter_Decorator_Ds_Health_Regen",
                id));
}
