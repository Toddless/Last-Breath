namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

    /// <summary>
    /// L2: on cast, applies "Reinforced Defense" (critical-damage mitigation) to the caster.
    /// </summary>
    public class CcAugmentApplyEnhancedDefence(string id, string[] tags, int tier, int duration, int maxStacks, float value)
        : AbilityAugment<CriticalCalculation>(id, tags, tier)
    {
        private readonly IActivationRider _modifier =
            new AbilityBuffActivationRider(new EnhanceDefenseEffect(duration, maxStacks, value));

        public override void ApplyUpgrade(CriticalCalculation ability) => ability.ActivationRiders.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.ActivationRiders.Remove(_modifier.Id);

        public override IAbilityAugment Copy() => new CcAugmentApplyEnhancedDefence(Id, Tags, Tier, duration, maxStacks, value);
    }
}
