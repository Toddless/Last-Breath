namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Enums;
    using Decorators;

    public class SoAsUpgradeMoreAttackDamage(string id, string[] tags, int tier, float multiplier)
        : SimpleUpgrade<SeriesOfAttacks, SeriesOfAttacks.Parameters>(id, tags, tier,
            new SimpleAbilityParameterDecorator<SeriesOfAttacks.Parameters>(
                SeriesOfAttacks.Parameters.DamageMultiplier,
                Priority.Weak,
                OperationType.Add, multiplier,
                "Ability_Parameter_Decorator_SoA_Attack_Damage",
                id));
}
