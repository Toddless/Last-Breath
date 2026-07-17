namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class IpUpgradeAmountAttacks(string id, string[] tags, int tier, int additionalAttacks)
        : SimpleUpgrade<IncreasingPressure>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                IncreasingPressure.Parameters.Attacks,
                Priority.Weak,
                OperationType.Add,
                additionalAttacks,
                "Ability_Parameter_Decorator_Ip_Additional_Attacks",
                id))
    {
    }
}
