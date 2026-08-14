namespace Battle.Source.Abilities.PoisonCoating
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>L2 upgrade: poison stacks applied by the coating last additional turns.</summary>
    public class PcAugmentAdditionalPoisonDuration(string id, string[] tags, int tier, int additionalDuration)
        : SimpleAugment<PoisonCoating>(id, tags, tier, new SimpleAbilityParameterDecorator(
            AbilityParameter.PoisonDuration,
            Priority.Weak,
            OperationType.Add,
            additionalDuration,
            "Ability_Parameter_Decorator_Pc_Poison_Duration",
            id));
}
