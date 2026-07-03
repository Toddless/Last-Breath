namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Decorators;

    public class SoAsUpgradeAdditionalAttacks(string id, string[] tags, int tier, int amountAttacks)
        : AbilityUpgrade<SeriesOfDamagings>(id, tags, tier)
    {
        private const string MinAttacksDecoratorId = "Ability_Parameter_Decorator_SoA_Min_Attacks";
        private const string MaxAttacksDecoratorId = "Ability_Parameter_Decorator_SoA_Max_Attacks";

        public override void ApplyUpgrade(SeriesOfDamagings ability)
        {
            var minAttacksDecorator = new SimpleAbilityParameterDecorator<SeriesOfDamagings.Parameters>(SeriesOfDamagings.Parameters.MinAttacks, Priority.Weak, OperationType.Add,
                amountAttacks, MinAttacksDecoratorId, Id);
            var maxAttacksDecorator = new SimpleAbilityParameterDecorator<SeriesOfDamagings.Parameters>(SeriesOfDamagings.Parameters.MaxAttacks, Priority.Weak, OperationType.Add,
                amountAttacks, MaxAttacksDecoratorId, Id);
            ability.AddParameterDecorator(minAttacksDecorator);
            ability.AddParameterDecorator(maxAttacksDecorator);
        }

        public override void RemoveUpgrade(SeriesOfDamagings ability)
        {
            ability.RemoveParameterDecorator(MinAttacksDecoratorId, SeriesOfDamagings.Parameters.MinAttacks);
            ability.RemoveParameterDecorator(MaxAttacksDecoratorId, SeriesOfDamagings.Parameters.MaxAttacks);
        }

        public override IAbilityUpgrade Copy() => new SoAsUpgradeAdditionalAttacks(Id, Tags, Tier, amountAttacks);
    }
}
