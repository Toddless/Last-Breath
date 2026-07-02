namespace Battle.Source.Abilities.PoisonExplosion
{
    using Core.Interfaces.Abilities;
    using Effects;

    /// <summary>
    /// L3 upgrade: on cast, applies a Seal of Oblivion debuff to the targets.
    /// </summary>
    public class PeUpgradeApplySealOfOblivion(string id, string[] tags, int tier, int duration, int maxStacks)
        : AbilityUpgrade<PoisonExplosion>(id, tags, tier)
    {
        private readonly IAbilityPostActivationModifier _modifier =
            new AbilityDebuffPostActivationModifier(new SealOfOblivion("Effect_Seal_Of_Oblivion", duration, maxStacks));

        public override void ApplyUpgrade(PoisonExplosion ability) => ability.PostActivationEffect.TryAdd(_modifier.Id, _modifier);

        public override void RemoveUpgrade(PoisonExplosion ability) => ability.PostActivationEffect.Remove(_modifier.Id);

        public override IAbilityUpgrade Copy() => new PeUpgradeApplySealOfOblivion(Id, Tags, Tier, duration, maxStacks);
    }
}
