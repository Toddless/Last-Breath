namespace Battle.Source.Abilities.DarkShroud
{
    using System;
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>L3 "Immortality": the shroud puts the Life Aegis on its caster — the dodge of a first
    /// death. Built per cast, so the effect reads the ability's effectiveness instead of a figure the
    /// augment was seated with, and its numbers come from the canon rather than from the record.</summary>
    public class AugmentDsImmortality(string id, string[] tags, int tier, Func<IEffect?> effect)
        : Augment<DarkShroud>(id, tags, tier)
    {
        /// <summary>What the shroud files this rider under — the name AbilityBuffActivationRider would
        /// have derived from the effect's own id.</summary>
        private const string RiderId = "Ability_Apply_Effect_Evade_First_Death_Activation_Rider";

        public override void ApplyUpgrade(DarkShroud ability) =>
            ability.AddActivationRider(RiderKey(RiderId), new DeferredEffectActivationRider(RiderId, effect));

        public override void RemoveUpgrade(DarkShroud ability) => ability.RemoveActivationRider(RiderKey(RiderId));

        public override IAugment Copy() => new AugmentDsImmortality(Id, Tags, Tier, effect);
    }
}
