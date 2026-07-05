namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Interfaces.Abilities;
    using Effects;
    using Modifiers;

    /// <summary>
    /// L3: for the ability's buff duration, the caster's critical chance becomes "Lucky".
    /// </summary>
    public class CcUpgradeLuckyCrit(string id, string[] tags, int tier, int duration)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private readonly IActivationRider _modifier =
            new AbilityBuffPostActivationModifier(new LuckyCritChanceEffect(duration, 1));

        public override void ApplyUpgrade(CriticalCalculation ability) => ability.ActivationRiders.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(CriticalCalculation ability) => ability.ActivationRiders.Remove(_modifier.Id);

        public override IAbilityUpgrade Copy() => new CcUpgradeLuckyCrit(Id, Tags, Tier, duration);
    }
}
