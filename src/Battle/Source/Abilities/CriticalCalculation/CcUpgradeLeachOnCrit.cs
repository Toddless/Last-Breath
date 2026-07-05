namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Interfaces.Abilities;
    using Effects;
    using Modifiers;
    using Riders;

    /// <summary>
    /// L2: while the ability's buff is active, the caster's critical attacks heal it for a percentage
    /// of the damage dealt. Implemented as a timed effect that registers a crit-leech attack modifier.
    /// </summary>
    public class CcUpgradeLeachOnCrit(string id, string[] tags, int tier, float amount, int duration)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private string _modifierId = string.Empty;

        public override void ApplyUpgrade(CriticalCalculation ability)
        {
            var modifier = new AbilityBuffActivationRider(new CritLeechEffect(duration, 1, amount));
            _modifierId = modifier.Id;
            ability.ActivationRiders.TryAdd(modifier.Id, modifier);
        }

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.ActivationRiders.Remove(_modifierId);

        public override IAbilityUpgrade Copy() => new CcUpgradeLeachOnCrit(Id, Tags, Tier, amount, duration);
    }
}
