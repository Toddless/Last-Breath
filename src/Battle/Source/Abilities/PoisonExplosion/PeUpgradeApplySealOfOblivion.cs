namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Battle.Abilities;
    using Effects;
    using Riders;

    /// <summary>
    /// L3 upgrade: on cast, applies a Seal of Oblivion debuff to the targets.
    /// </summary>
    public class PeUpgradeApplySealOfOblivion(string id, string[] tags, int tier, int duration, int maxStacks)
        : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        private readonly IActivationRider _modifier =
            new AbilityDebuffActivationRider(new OblivionSeal(duration, maxStacks));

        public override void ApplyUpgrade(PoisonExplosion ability) => ability.ActivationRiders.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.ActivationRiders.Remove(_modifier.Id);

        public override IAbilityUpgrade Copy() => new PeUpgradeApplySealOfOblivion(Id, Tags, Tier, duration, maxStacks);
    }
}
