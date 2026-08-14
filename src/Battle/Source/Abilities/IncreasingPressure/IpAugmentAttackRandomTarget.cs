namespace Battle.Source.Abilities.IncreasingPressure
{
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>L3 upgrade: each successful attack also deals a percentage of the damage to a random enemy.</summary>
    public class IpAugmentAttackRandomTarget(string id, string[] tags, int tier, float splashPercent)
        : AbilityAugment<IncreasingPressure>(id, tags, tier)
    {
        private readonly IImpactRider _rider = new SplashRandomTargetRider(splashPercent);

        public override void ApplyUpgrade(IncreasingPressure ability) => ability.ImpactRiders.TryAdd(_rider.Id, _rider);

        public override void RemoveUpgrade(IncreasingPressure ability) => ability.ImpactRiders.Remove(_rider.Id);

        public override IAbilityAugmentWrap<IncreasingPressure> Copy() =>
            new IpAugmentAttackRandomTarget(Id, Tags, Tier, splashPercent);
    }
}
