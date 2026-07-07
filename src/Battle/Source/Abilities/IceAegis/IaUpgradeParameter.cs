namespace Battle.Source.Abilities.IceAegis
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    /// <summary>Additive bump of one Ice Aegis parameter (base barrier / per-intelligence scale / duration).</summary>
    public class IaUpgradeParameter(string id, string[] tags, int tier, IceAegis.Parameters parameter, float amount)
        : AbilityUpgrade<IceAegis>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_Ia_{parameter}";

        public override void ApplyUpgrade(IceAegis ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<IceAegis.Parameters>(
                parameter, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(IceAegis ability) =>
            ability.RemoveParameterDecorator(DecoratorId, parameter);

        public override IAbilityUpgrade Copy() => new IaUpgradeParameter(Id, Tags, Tier, parameter, amount);
    }
}
