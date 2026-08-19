namespace Battle.Source.Abilities.IceAegis
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Additive bump of one Ice Aegis parameter (base barrier / per-intelligence scale / duration).</summary>
    public class IaAugmentParameter(string id, string[] tags, int tier, string parameter, float amount)
        : AbilityAugment<IceAegis>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_Ia_{parameter}";

        public override void ApplyUpgrade(IceAegis ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                parameter, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(IceAegis ability) =>
            ability.RemoveParameterDecorator(DecoratorId, parameter);

        public override IAbilityAugment Copy() => new IaAugmentParameter(Id, Tags, Tier, parameter, amount);
    }
}
