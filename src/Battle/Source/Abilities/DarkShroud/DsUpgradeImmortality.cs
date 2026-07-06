namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Interfaces.Abilities;
    using Effects;
    using Riders;

    /// <summary>
    /// L3 upgrade ("Immortality"): while the shroud is active, the caster is under Life-Giving Shade
    /// </summary>
    public class DsUpgradeImmortality(string id, string[] tags, int tier, float lifeToRecover, int duration, int activations)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        private readonly IActivationRider _modifier =
            new AbilityBuffActivationRider(new LifeGivingShadeEffect(lifeToRecover, duration, activations));

        public override void ApplyUpgrade(DarkShroud ability) => ability.ActivationRiders.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(DarkShroud ability) => ability.ActivationRiders.Remove(_modifier.Id);

        public override IAbilityUpgrade Copy() => new DsUpgradeImmortality(Id, Tags, Tier, lifeToRecover, duration, activations);
    }
}
