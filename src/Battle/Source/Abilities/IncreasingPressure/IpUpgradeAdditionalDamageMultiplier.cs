namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class IpUpgradeAdditionalDamageMultiplier(string id, string[] tags, int tier, float additionalMultiplier)
        : SimpleUpgrade<IncreasingPressure>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                IncreasingPressure.Parameters.AttackDamageStepMultiplier,
                Priority.Weak,
                OperationType.Add,
                additionalMultiplier,
                "Ability_Parameter_Decorator_Ip_Additional_Damage_Multiplier",
                id));
}
