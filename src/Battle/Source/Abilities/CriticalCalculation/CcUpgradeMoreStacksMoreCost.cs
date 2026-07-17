namespace Battle.Source.Abilities.CriticalCalculation
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// L1: increases the number of buff stacks applied and raises the ability's mana cost.
    /// </summary>
    public class CcUpgradeMoreStacksMoreCost(string id, string[] tags, int tier, int stacks, float cost)
        : AbilityUpgrade<CriticalCalculation>(id, tags, tier)
    {
        private const string StacksDecoratorId = "Ability_Parameter_Decorator_Cc_Buff_Stacks";
        private const string CostDecoratorId = "Ability_Parameter_Decorator_Cc_Cost";

        public override void ApplyUpgrade(CriticalCalculation ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                CriticalCalculation.Parameters.Stacks, Priority.Weak, OperationType.Add, stacks, StacksDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.CostValue, Priority.Weak, OperationType.Add, cost, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(CriticalCalculation ability)
        {
            ability.RemoveParameterDecorator(StacksDecoratorId, CriticalCalculation.Parameters.Stacks);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAbilityUpgrade Copy() => new CcUpgradeMoreStacksMoreCost(Id, Tags, Tier, stacks, cost);
    }
}
