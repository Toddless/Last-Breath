namespace Battle.TestData.Abilities.Decorators
{
    using Core.Enums;

    public class AbilityParameterChangeCostTypeDecorator(string id, Costs type)
        : AbilityParameterDecorator(abilityParameter: AbilityParameter.CostType, priority: DecoratorPriority.Absolute, id)
    {
        public override float GetValue()
        {
            base.GetValue();
            return (float)type;
        }
    }
}
