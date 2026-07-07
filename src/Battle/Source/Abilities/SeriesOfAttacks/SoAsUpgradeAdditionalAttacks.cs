namespace Battle.Source.Abilities.SeriesOfAttacks
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    public class SoAsUpgradeAdditionalAttacks(string id, string[] tags, int tier, int amountAttacks)
        : AbilityUpgrade<SeriesOfAttacks>(id, tags, tier)
    {
        private const string MinAttacksDecoratorId = "Ability_Parameter_Decorator_SoA_Min_Attacks";
        private const string MaxAttacksDecoratorId = "Ability_Parameter_Decorator_SoA_Max_Attacks";

        public override void ApplyUpgrade(SeriesOfAttacks ability)
        {
            var minAttacksDecorator = new SimpleAbilityParameterDecorator<SeriesOfAttacks.Parameters>(SeriesOfAttacks.Parameters.MinAttacks, Priority.Weak, OperationType.Add,
                amountAttacks, MinAttacksDecoratorId, Id);
            var maxAttacksDecorator = new SimpleAbilityParameterDecorator<SeriesOfAttacks.Parameters>(SeriesOfAttacks.Parameters.MaxAttacks, Priority.Weak, OperationType.Add,
                amountAttacks, MaxAttacksDecoratorId, Id);
            ability.AddParameterDecorator(minAttacksDecorator);
            ability.AddParameterDecorator(maxAttacksDecorator);
        }

        public override void RemoveUpgrade(SeriesOfAttacks ability)
        {
            ability.RemoveParameterDecorator(MinAttacksDecoratorId, SeriesOfAttacks.Parameters.MinAttacks);
            ability.RemoveParameterDecorator(MaxAttacksDecoratorId, SeriesOfAttacks.Parameters.MaxAttacks);
        }

        public override IAbilityUpgrade Copy() => new SoAsUpgradeAdditionalAttacks(Id, Tags, Tier, amountAttacks);
    }
}
