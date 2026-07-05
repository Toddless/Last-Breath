namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Interfaces.Abilities;
    using Riders;

    /// <summary>L3 upgrade: remaining poison stacks transfer to another enemy when the target dies.</summary>
    public class JoPUpgradeTransferOnDeath(string id, string[] tags, int tier)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        private readonly IImpactRider _rider = new TransferPoisonOnDeathRider();

        public override void ApplyUpgrade(JarOfPoison ability) => ability.ImpactRiders.TryAdd(_rider.Id, _rider);

        public override void RemoveUpgrade(JarOfPoison ability) => ability.ImpactRiders.Remove(_rider.Id);

        public override IAbilityUpgradeWrap<JarOfPoison> Copy() =>
            new JoPUpgradeTransferOnDeath(Id, Tags, Tier);
    }
}
