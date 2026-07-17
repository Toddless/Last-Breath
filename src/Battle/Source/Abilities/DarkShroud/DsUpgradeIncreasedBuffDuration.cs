namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// L1 upgrade: increases the shroud's buff duration.
    /// </summary>
    public class DsUpgradeIncreasedBuffDuration(string id, string[] tags, int tier, float duration)
        : SimpleUpgrade<DarkShroud>(id, tags, tier,
            new SimpleAbilityParameterDecorator(
                DarkShroud.Parameters.Duration,
                Priority.Weak,
                OperationType.Add,
                duration,
                "Ability_Parameter_Decorator_Ds_Duration",
                id));
}
