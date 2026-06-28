namespace Battle.Source.Abilities
{
    using System;
    using Core.Interfaces.Abilities;
    using Decorators;

    public class SimpleUpgrade<TAbility, TParameter>(string id, string[] tags, int tier, AbilityParameterDecorator<TParameter> decorator)
        : AbilityUpgrade<TAbility>(id, tags, tier)
        where TAbility : IAbility
        where TParameter : struct, Enum
    {
        public override void ApplyUpgrade(TAbility ability) => ability.AddParameterDecorator(decorator);

        public override void RemoveUpgrade(TAbility ability) => ability.RemoveParameterDecorator(decorator.Id, decorator.Parameter);

        public override IAbilityUpgradeWrap<TAbility> Copy() => new SimpleUpgrade<TAbility, TParameter>(Id, Tags, Tier, decorator);
    }
}
