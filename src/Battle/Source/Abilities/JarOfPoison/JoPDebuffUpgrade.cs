namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Interfaces.Abilities;

    /// <summary>L2 upgrades: the debuff rides every landing of the jar — each touched target gets a stack.</summary>
    public class JoPDebuffUpgrade(string id, string[] tags, int tier, IImpactRider rider)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        public override void ApplyUpgrade(JarOfPoison ability) => ability.ImpactRiders.TryAdd(rider.Id, rider);

        public override void RemoveUpgrade(JarOfPoison ability) => ability.ImpactRiders.Remove(rider.Id);

        public override IAbilityUpgrade Copy() => new JoPDebuffUpgrade(Id, Tags, Tier, rider);
    }
}
