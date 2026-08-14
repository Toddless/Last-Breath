namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Battle.Abilities;
    using Riders;

    /// <summary>L3 upgrade: remaining poison stacks transfer to another enemy when the target dies.</summary>
    public class JoPAugmentTransferOnDeath(string id, string[] tags, int tier)
        : AbilityAugment<JarOfPoison>(id, tags, tier)
    {
        private readonly IImpactRider _rider = new TransferPoisonOnDeathRider();

        public override void ApplyUpgrade(JarOfPoison ability) => ability.ImpactRiders.TryAdd(_rider.Id, _rider);

        public override void RemoveUpgrade(JarOfPoison ability) => ability.ImpactRiders.Remove(_rider.Id);

        public override IAbilityAugmentWrap<JarOfPoison> Copy() =>
            new JoPAugmentTransferOnDeath(Id, Tags, Tier);
    }
}
