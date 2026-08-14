namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;

    /// <summary>Generic upgrade: attaches an activation rider to the ability — it fires once per cast.
    /// Counterpart of <see cref="AbilityAugmentImpactRider"/>.</summary>
    public class AbilityAugmentActivationRider(string id, string[] tags, int tier, IActivationRider rider)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability) => ability.ActivationRiders.TryAdd(rider.Id, rider);

        public override void RemoveUpgrade(Ability ability) => ability.ActivationRiders.Remove(rider.Id);

        public override IAbilityAugment Copy() => new AbilityAugmentActivationRider(Id, Tags, Tier, rider);
    }
}
