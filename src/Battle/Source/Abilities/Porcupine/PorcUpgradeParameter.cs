namespace Battle.Source.Abilities.Porcupine
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    /// <summary>Additive bump of one Porcupine parameter (returns / heal share / cooldown chance / armor).</summary>
    public class PorcUpgradeParameter(string id, string[] tags, int tier, Porcupine.Parameters parameter, float amount)
        : AbilityUpgrade<Porcupine>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_Porc_{parameter}";

        public override void ApplyUpgrade(Porcupine ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<Porcupine.Parameters>(
                parameter, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(Porcupine ability) =>
            ability.RemoveParameterDecorator(DecoratorId, parameter);

        public override IAbilityUpgrade Copy() => new PorcUpgradeParameter(Id, Tags, Tier, parameter, amount);
    }
}
