namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;

    public class SimpleUpgrade<TAbility>(string id, string[] tags, int tier, AbilityParameterDecorator decorator)
        : AbilityUpgrade<TAbility>(id, tags, tier)
        where TAbility : IAbility
    {
        public override void ApplyUpgrade(TAbility ability) => ability.AddParameterDecorator(decorator);

        public override void RemoveUpgrade(TAbility ability) => ability.RemoveParameterDecorator(decorator.Id, decorator.Parameter);

        public override IAbilityUpgradeWrap<TAbility> Copy() => new SimpleUpgrade<TAbility>(Id, Tags, Tier, decorator);
    }
}
