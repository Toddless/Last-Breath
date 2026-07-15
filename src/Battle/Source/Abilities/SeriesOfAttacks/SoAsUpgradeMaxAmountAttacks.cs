namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    public class SoAsUpgradeMaxAmountAttacks(string id, string[] tags, int tier, int additionalAmountAttacks)
        : SimpleUpgrade<SeriesOfAttacks, SeriesOfAttacks.Parameters>(id, tags, tier,
            new SimpleAbilityParameterDecorator<SeriesOfAttacks.Parameters>(
                SeriesOfAttacks.Parameters.MaxAttacks,
                Priority.Weak,
                OperationType.Add,
                additionalAmountAttacks,
                "Ability_Parameter_Decorator_SoA_Max_Attacks",
                id));
}
