namespace Battle.Source.Abilities.PoisonCoating
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>L1 upgrade: the coating buff itself lasts additional turns.</summary>
    public class PcUpgradeIncreaseDuration(string id, string[] tags, int tier, int duration)
        : SimpleUpgrade<PoisonCoating>(id, tags, tier, new SimpleAbilityParameterDecorator(
            PoisonCoating.Parameters.CoatingDuration,
            Priority.Weak,
            OperationType.Add,
            duration,
            "Ability_Parameter_Decorator_Pc_Coating_Duration",
            id));
}
