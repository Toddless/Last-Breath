namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;

    /// <summary>Generic upgrade: attaches an activation rider to the ability — it fires once per cast.
    /// Counterpart of <see cref="AbilityUpgradeImpactRider"/>.</summary>
    public class AbilityUpgradeActivationRider(string id, string[] tags, int tier, IActivationRider rider)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability) => ability.ActivationRiders.TryAdd(rider.Id, rider);

        public override void RemoveUpgrade(Ability ability) => ability.ActivationRiders.Remove(rider.Id);

        public override IAbilityUpgrade Copy() => new AbilityUpgradeActivationRider(Id, Tags, Tier, rider);
    }
}
