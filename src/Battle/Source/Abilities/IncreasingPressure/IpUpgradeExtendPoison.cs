namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Interfaces.Abilities;
    using Riders;

    /// <summary>L2 upgrade: each successful attack extends the poison duration on the target by 1 turn.</summary>
    public class IpUpgradeExtendPoison(string id, string[] tags, int tier, int extensionDuration)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        private readonly IImpactRider _rider = new ExtendPoisonOnHitRider(extensionDuration);

        public override void ApplyUpgrade(IncreasingPressure ability) => ability.ImpactRiders.TryAdd(_rider.Id, _rider);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.ImpactRiders.Remove(_rider.Id);

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeExtendPoison(Id, Tags, Tier, extensionDuration);
    }
}
