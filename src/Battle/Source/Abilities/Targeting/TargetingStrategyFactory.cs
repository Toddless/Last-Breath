namespace Battle.Source.Abilities.Targeting
{
    using System;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Enums;

    /// <summary>Builds the default targeting strategy from ability data. Abilities that target randomly
    /// (or otherwise specially) set their own strategy in <see cref="AbilityProvider"/> instead.</summary>
    public static class TargetingStrategyFactory
    {
        public static ITargetingStrategy From(AbilityBaseData data) => data.TargetType switch
        {
            AbilityTargetType.Enemy => new SingleTargetTargeting(TargetRelation.Enemies),
            AbilityTargetType.Ally => new SingleTargetTargeting(TargetRelation.Allies),
            AbilityTargetType.FewEnemies => new FewTargetsTargeting(TargetRelation.Enemies, data.MaxTargets),
            AbilityTargetType.FewAllies => new FewTargetsTargeting(TargetRelation.Allies, data.MaxTargets),
            AbilityTargetType.Self => new SelfTargeting(),
            _ => throw new ArgumentOutOfRangeException(nameof(data), data.TargetType, "Unknown ability target type")
        };
    }
}
