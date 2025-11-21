namespace Battle.TestData.Abilities.Decorators
{
    using Godot;
    using System;
    using Core.Enums;

    public class AbilityParameterChangeCostValueDecorator(Func<float> cost, bool increaseCost = false)
        : AbilityParameterDecorator(abilityParameter: AbilityParameter.CostValue, priority: DecoratorPriority.Strong, id: "Ability_Parameter_Change_Cost_Value_Decorator")
    {
        public override float GetValue() => Mathf.Max(0, increaseCost ? base.GetValue() + cost() : base.GetValue() - cost());
    }
}
