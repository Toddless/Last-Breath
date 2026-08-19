namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;

    /// <summary>
    /// Generic upgrade: attaches an impact rider to the ability — it fires on every delivery impact.
    ///
    /// <para>The rider arrives as a FACTORY and not as an object, because a copy of this augment must
    /// get a rider of its own. A rider now holds things it has to be told to let go of
    /// (<see cref="IImpactRider.Detach"/>), so two augments sharing one would mean the rebuild of one
    /// ability tearing down a subscription the other ability's rider is still using.</para>
    /// </summary>
    public class AbilityAugmentImpactRider(string id, string[] tags, int tier, Func<IImpactRider> riderFactory)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        private IImpactRider? _rider;

        /// <summary>This augment's own rider, minted once: seating and unseating have to name the same
        /// object, and a fresh one on every call would seat one rider and take another off.</summary>
        private IImpactRider Rider => _rider ??= riderFactory();

        public override void ApplyUpgrade(Ability ability) => ability.AddImpactRider(RiderKey(Rider.Id), Rider);

        public override void RemoveUpgrade(Ability ability) => ability.RemoveImpactRider(RiderKey(Rider.Id));

        public override IAbilityAugment Copy() => new AbilityAugmentImpactRider(Id, Tags, Tier, riderFactory);
    }
}
