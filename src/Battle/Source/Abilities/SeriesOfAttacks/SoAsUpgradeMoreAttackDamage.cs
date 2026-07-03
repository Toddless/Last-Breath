namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Enums;
    using Decorators;

    public class SoAsUpgradeMoreAttackDamage(string id, string[] tags, int tier, float multiplier)
        : SimpleUpgrade<SeriesOfDamagings, SeriesOfDamagings.Parameters>(id, tags, tier,
            new SimpleAbilityParameterDecorator<SeriesOfDamagings.Parameters>(
                SeriesOfDamagings.Parameters.DamageMultiplier,
                Priority.Weak,
                OperationType.Add, multiplier,
                "Ability_Parameter_Decorator_SoA_Attack_Damage",
                id));
}
