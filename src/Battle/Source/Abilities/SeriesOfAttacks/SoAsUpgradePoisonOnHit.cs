namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>L3 upgrade: every successful attack of the series puts a poison stack on the target.</summary>
    public class SoAsUpgradePoisonOnHit(string id, string[] tags, int tier, int poisonDuration)
        : AbilityUpgrade<SeriesOfAttacks>(id, tags, tier)
    {
        private readonly IImpactRider _rider = new PoisonOnHitRider(poisonDuration);

        public override void ApplyUpgrade(SeriesOfAttacks ability) => ability.ImpactRiders.TryAdd(_rider.Id, _rider);

        public override void RemoveUpgrade(SeriesOfAttacks ability) => ability.ImpactRiders.Remove(_rider.Id);

        public override IAbilityUpgradeWrap<SeriesOfAttacks> Copy() => new SoAsUpgradePoisonOnHit(Id, Tags, Tier, poisonDuration);
    }
}
