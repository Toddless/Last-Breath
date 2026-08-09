namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

    /// <summary>
    /// L3 upgrade ("Immortality"): while the shroud is active, the caster is under Life-Giving Shade.
    ///
    /// What the shade restores is read at the moment of the cast rather than fixed when the augment was
    /// seated, and it is read through the ability's effectiveness — the shade is content the shroud
    /// lays, so a record offering "what you lay lands harder" is offering exactly this. Held at
    /// construction the figure would be the figure forever, and an effectiveness augment beside it
    /// would be moving a number nothing reads.
    /// </summary>
    public class DsUpgradeImmortality(string id, string[] tags, int tier, float lifeToRecover, int duration, int activations)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        /// <summary>What the shroud files this rider under. The name a rider building its own effect
        /// carries has to be written down somewhere, and it is written here rather than remembered in a
        /// field: it is the same string for every copy of this augment and every ability it is seated
        /// on, so remembering it per instance would be storing a constant. It reads as
        /// <c>AbilityBuffActivationRider</c> would have derived it from
        /// <see cref="LifeGivingShadeEffect"/>'s own id, which is what keeps the two lists — the riders
        /// an ability wears and the effects they lay — spelling one another the same way.</summary>
        private const string RiderId = "Ability_Apply_Effect_Life_Giving_Shade_Activation_Rider";

        public override void ApplyUpgrade(DarkShroud ability) =>
            ability.ActivationRiders.TryAdd(RiderId, new DeferredEffectActivationRider(
                RiderId,
                () => new LifeGivingShadeEffect(lifeToRecover * ability.Effectiveness, duration, activations)));

        public override void RemoveUpgrade(DarkShroud ability) => ability.ActivationRiders.Remove(RiderId);

        public override IAbilityUpgrade Copy() => new DsUpgradeImmortality(Id, Tags, Tier, lifeToRecover, duration, activations);
    }
}
