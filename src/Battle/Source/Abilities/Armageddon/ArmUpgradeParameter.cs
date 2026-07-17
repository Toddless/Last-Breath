namespace Battle.Source.Abilities.Armageddon
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Additive bump of one Armageddon parameter (stun duration / hp-cost multiplier / missing-hp rate).</summary>
    public class ArmUpgradeParameter(string id, string[] tags, int tier, string parameter, float amount)
        : AbilityUpgrade<Armageddon>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_Arm_{parameter}";

        public override void ApplyUpgrade(Armageddon ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                parameter, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(Armageddon ability) =>
            ability.RemoveParameterDecorator(DecoratorId, parameter);

        public override IAbilityUpgrade Copy() => new ArmUpgradeParameter(Id, Tags, Tier, parameter, amount);
    }
}
