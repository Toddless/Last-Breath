namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>L2 upgrade: lowers the poison stack count required for execution by 5.</summary>
    public class PeUpgradeLowerExecutionThreshold(string id, string[] tags, int tier, int reduction)
        : SimpleUpgrade<PoisonExplosion, PoisonExplosion.Parameters>(id, tags, tier,
            new SimpleAbilityParameterDecorator<PoisonExplosion.Parameters>(
            PoisonExplosion.Parameters.ExecutionThreshold,
            Priority.Weak,
            OperationType.Subtract,
            reduction,
            "Ability_Parameter_Decorator_Pe_Execution_Threshold",
            id));
}
