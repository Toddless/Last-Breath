namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Interfaces.Abilities;
    using Effects;
    using Modifiers;

    /// <summary>
    /// L2: on cast, applies "Reinforced Defense" (critical-damage mitigation) to the caster.
    /// </summary>
    public class CcUpgradeApplyEnhancedDefence(string id, string[] tags, int tier, int duration, int maxStacks, float value)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private readonly IActivationRider _modifier =
            new AbilityBuffPostActivationModifier(new EnhanceDefenseEffect(duration, maxStacks, value));

        public override void ApplyUpgrade(CriticalCalculation ability) => ability.ActivationRiders.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.ActivationRiders.Remove(_modifier.Id);

        public override IAbilityUpgrade Copy() => new CcUpgradeApplyEnhancedDefence(Id, Tags, Tier, duration, maxStacks, value);
    }
}
