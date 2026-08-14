namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;

    /// <summary>Generic upgrade: attaches an impact rider to the ability — it fires on every delivery impact.</summary>
    public class AbilityAugmentImpactRider(string id, string[] tags, int tier, IImpactRider rider)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability) => ability.ImpactRiders.TryAdd(rider.Id, rider);

        public override void RemoveUpgrade(Ability ability) => ability.ImpactRiders.Remove(rider.Id);

        public override IAbilityAugment Copy() => new AbilityAugmentImpactRider(Id, Tags, Tier, rider);
    }
}
