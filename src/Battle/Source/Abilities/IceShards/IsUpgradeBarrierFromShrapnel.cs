namespace Battle.Source.Abilities.IceShrapnel
{
    using Core.Enums;
    using Decorators;
    using IceShards;

    /// <summary>L3 upgrade: restores barrier equal to a fraction of the shrapnel burst damage (raises a zero base parameter).</summary>
    public class IsUpgradeBarrierFromShrapnel(string id, string[] tags, int tier, float leachPercent)
        : SimpleUpgrade<IceShards, IceShards.Parameters>(id, tags, tier, new SimpleAbilityParameterDecorator<IceShards.Parameters>(
            IceShards.Parameters.ShrapnelBarrierLeach,
            Priority.Weak,
            OperationType.Add,
            leachPercent,
            "Ability_Parameter_Decorator_Is_Shrapnel_Barrier_Leach",
            id));
}
