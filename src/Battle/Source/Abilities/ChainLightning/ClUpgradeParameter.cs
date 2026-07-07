namespace Battle.Source.Abilities.ChainLightning
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    /// <summary>Additive bump of one Chain Lightning parameter (jumps / falloff — pass a negative amount to soften).</summary>
    public class ClUpgradeParameter(string id, string[] tags, int tier, ChainLightning.Parameters parameter, float amount)
        : AbilityUpgrade<ChainLightning>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_Cl_{parameter}";

        public override void ApplyUpgrade(ChainLightning ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<ChainLightning.Parameters>(
                parameter, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(ChainLightning ability) =>
            ability.RemoveParameterDecorator(DecoratorId, parameter);

        public override IAbilityUpgrade Copy() => new ClUpgradeParameter(Id, Tags, Tier, parameter, amount);
    }
}
