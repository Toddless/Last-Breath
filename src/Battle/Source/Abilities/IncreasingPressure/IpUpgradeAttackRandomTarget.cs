namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>L3 upgrade: each successful attack also deals a percentage of the damage to a random enemy.</summary>
    public class IpUpgradeAttackRandomTarget(string id, string[] tags, int tier, float splashPercent)
        : AbilityUpgrade<IncreasingPressure>(id, tags, tier)
    {
        private readonly IImpactRider _rider = new SplashRandomTargetRider(splashPercent);

        public override void ApplyUpgrade(IncreasingPressure ability) => ability.ImpactRiders.TryAdd(_rider.Id, _rider);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.ImpactRiders.Remove(_rider.Id);

        public override IAbilityUpgradeWrap<IncreasingPressure> Copy() =>
            new IpUpgradeAttackRandomTarget(Id, Tags, Tier, splashPercent);
    }
}
