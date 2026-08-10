namespace Battle.Source.Abilities.DarkShroud
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

    /// <summary>L3 "Immortality": the shroud puts Life-Giving Shade on its caster. Built per cast, so
    /// what it restores follows the ability's effectiveness instead of the figure it was seated with.</summary>
    public class DsUpgradeImmortality(string id, string[] tags, int tier, float lifeToRecover, int duration, int activations)
        : AbilityUpgrade<DarkShroud>(id, tags, tier)
    {
        /// <summary>What the shroud files this rider under — the name AbilityBuffActivationRider would
        /// have derived from the effect's own id.</summary>
        private const string RiderId = "Ability_Apply_Effect_Life_Giving_Shade_Activation_Rider";

        public override void ApplyUpgrade(DarkShroud ability) =>
            ability.ActivationRiders.TryAdd(RiderId, new DeferredEffectActivationRider(
                RiderId,
                () => new LifeGivingShadeEffect(lifeToRecover, duration, activations)));

        public override void RemoveUpgrade(DarkShroud ability) => ability.ActivationRiders.Remove(RiderId);

        public override IAbilityUpgrade Copy() => new DsUpgradeImmortality(Id, Tags, Tier, lifeToRecover, duration, activations);
    }
}
