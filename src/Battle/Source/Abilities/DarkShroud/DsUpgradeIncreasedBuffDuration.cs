namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Components.Decorator;
    using Core.Enums;

    /// <summary>
    /// L1 upgrade: increases the shroud's buff duration.
    /// </summary>
    public class DsUpgradeIncreasedBuffDuration(string id, string[] tags, int tier, float duration)
        : SimpleUpgrade<DarkShroud, DarkShroud.Parameters>(id, tags, tier,
            new SimpleAbilityParameterDecorator<DarkShroud.Parameters>(
                DarkShroud.Parameters.Duration,
                Priority.Weak,
                OperationType.Add,
                duration,
                "Ability_Parameter_Decorator_Ds_Duration",
                id));
}
