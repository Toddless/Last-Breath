namespace Battle.Source.Abilities
{
    using System;
    using Core.Battle.Abilities;

    /// <summary>Generic upgrade: attaches an activation rider to the ability — it fires once per cast.
    /// Counterpart of <see cref="AugmentImpactRider"/>, factory and all, for the same reason: a
    /// copy needs a rider of its own now that a rider can be told to let go of what it hooked.</summary>
    public class AugmentActivationRider(string id, string[] tags, int tier, Func<IActivationRider> riderFactory)
        : Augment<Ability>(id, tags, tier)
    {
        private IActivationRider? _rider;

        private IActivationRider Rider => _rider ??= riderFactory();

        public override void ApplyUpgrade(Ability ability) => ability.AddActivationRider(RiderKey(Rider.Id), Rider);

        public override void RemoveUpgrade(Ability ability) => ability.RemoveActivationRider(RiderKey(Rider.Id));

        public override IAugment Copy() => new AugmentActivationRider(Id, Tags, Tier, riderFactory);
    }
}
