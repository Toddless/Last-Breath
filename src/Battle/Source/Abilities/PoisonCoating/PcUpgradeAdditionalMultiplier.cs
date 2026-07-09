namespace Battle.Source.Abilities.PoisonCoating
{
    using Core.Components.Decorator;
    using Core.Enums;

    /// <summary>L2 upgrade: poison stacks applied by the coating get an additional damage multiplier.</summary>
    public class PcUpgradeAdditionalMultiplier(string id, string[] tags, int tier, float multiplier)
        : SimpleUpgrade<PoisonCoating, PoisonCoating.Parameters>(id, tags, tier, new SimpleAbilityParameterDecorator<PoisonCoating.Parameters>(
            PoisonCoating.Parameters.PoisonMultiplier,
            Priority.Weak,
            OperationType.Add,
            multiplier,
            "Ability_Parameter_Decorator_Pc_Poison_Multiplier",
            id));
}
