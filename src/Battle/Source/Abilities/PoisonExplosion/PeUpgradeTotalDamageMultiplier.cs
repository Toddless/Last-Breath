namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>L1 upgrade: increases the total explosion damage by an additional multiplier.</summary>
    public class PeUpgradeTotalDamageMultiplier(string id, string[] tags, int tier, float multiplier)
        : SimpleUpgrade<PoisonExplosion>(id, tags, tier, new SimpleAbilityParameterDecorator(
            PoisonExplosion.Parameters.DamageMultiplier,
            Priority.Weak,
            OperationType.Add,
            multiplier,
            "Ability_Parameter_Decorator_Pe_Damage_Multiplier",
            id));
}
