namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Battle.Abilities;
    using Core.Enums;
    public class SoAsUpgradeMoreAttackDamage(string id, string[] tags, int tier, float multiplier)
        : SimpleUpgrade<SeriesOfAttacks>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                SeriesOfAttacks.Parameters.DamageMultiplier,
                Priority.Weak,
                OperationType.Add, multiplier,
                "Ability_Parameter_Decorator_SoA_Attack_Damage",
                id));
}
