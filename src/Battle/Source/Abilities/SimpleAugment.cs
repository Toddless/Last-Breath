namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;

    public class SimpleAugment<TAbility>(string id, string[] tags, int tier, AbilityParameterDecorator decorator)
        : AbilityAugment<TAbility>(id, tags, tier)
        where TAbility : IAbility
    {
        public override void ApplyUpgrade(TAbility ability) => ability.AddParameterDecorator(decorator);

        public override void RemoveUpgrade(TAbility ability) => ability.RemoveParameterDecorator(decorator.Id, decorator.Parameter);

        public override IAbilityAugmentWrap<TAbility> Copy() => new SimpleAugment<TAbility>(Id, Tags, Tier, decorator);
    }
}
