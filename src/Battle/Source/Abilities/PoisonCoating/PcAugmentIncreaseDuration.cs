namespace Battle.Source.Abilities.PoisonCoating
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>L1 upgrade: the coating buff itself lasts additional turns.</summary>
    public class PcAugmentIncreaseDuration(string id, string[] tags, int tier, int duration)
        : SimpleAugment<PoisonCoating>(id, tags, tier, new SimpleAbilityParameterDecorator(
            AbilityParameter.Duration,
            Priority.Weak,
            OperationType.Add,
            duration,
            "Ability_Parameter_Decorator_Pc_Coating_Duration",
            id));
}
