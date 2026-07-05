namespace Battle.Source.Abilities.Sacrifice
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Decorators;

    /// <summary>Additive bump of one Sacrifice parameter (charges / rate / sacrificed share / heal share).</summary>
    public class SacUpgradeParameter(string id, string[] tags, int tier, Sacrifice.Parameters parameter, float amount)
        : AbilityUpgrade<Sacrifice>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_Sac_{parameter}";

        public override void ApplyUpgrade(Sacrifice ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<Sacrifice.Parameters>(
                parameter, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(Sacrifice ability) =>
            ability.RemoveParameterDecorator(DecoratorId, parameter);

        public override IAbilityUpgrade Copy() => new SacUpgradeParameter(Id, Tags, Tier, parameter, amount);
    }
}
