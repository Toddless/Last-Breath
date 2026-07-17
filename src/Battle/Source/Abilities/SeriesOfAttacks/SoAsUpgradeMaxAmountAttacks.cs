namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Battle.Abilities;
    using Core.Enums;

    public class SoAsUpgradeMaxAmountAttacks(string id, string[] tags, int tier, int additionalAmountAttacks)
        : SimpleUpgrade<SeriesOfAttacks>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                SeriesOfAttacks.Parameters.MaxAttacks,
                Priority.Weak,
                OperationType.Add,
                additionalAmountAttacks,
                "Ability_Parameter_Decorator_SoA_Max_Attacks",
                id));
}
