namespace Battle.Source.Abilities
{
    using Core.Interfaces.Abilities;

    /// <summary>Generic upgrade: attaches an impact rider to the ability — it fires on every delivery impact.</summary>
    public class AbilityUpgradeImpactRider(string id, string[] tags, int tier, IImpactRider rider)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        public override void ApplyUpgrade(Ability ability) => ability.ImpactRiders.TryAdd(rider.Id, rider);

        public override void RemoveUpgrade(Ability ability) => ability.ImpactRiders.Remove(rider.Id);

        public override IAbilityUpgrade Copy() => new AbilityUpgradeImpactRider(Id, Tags, Tier, rider);
    }
}
