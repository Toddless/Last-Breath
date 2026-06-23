namespace Battle.Source.Abilities.JarOfPoison
{
    using Core.Interfaces.Abilities;

    public class JoPDebuffUpgrade(string id, string[] tags, int tier, IAbilityPostActivationModifier modifier)
        : AbilityUpgrade<JarOfPoison>(id, tags, tier)
    {
        public override void ApplyUpgrade(JarOfPoison ability) => ability.PostActivationEffect.TryAdd(modifier.Id, modifier);

        public override void RemoveUpgrade(JarOfPoison ability) => ability.PostActivationEffect.Remove(modifier.Id);

        public override IAbilityUpgrade Clone() => new JoPDebuffUpgrade(Id, Tags, Tier, modifier);
    }
}
