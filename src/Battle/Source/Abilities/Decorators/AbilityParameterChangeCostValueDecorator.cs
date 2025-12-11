namespace Battle.Source.Abilities.Decorators
{
    using System;
    using Core.Enums;
    using Godot;

    public class AbilityParameterChangeCostValueDecorator(Func<float> cost, bool increaseCost = false)
        : AbilityParameterDecorator(abilityParameter: AbilityParameter.CostValue, priority: DecoratorPriority.Weak, id: "Ability_Parameter_Change_Cost_Value_Decorator")
    {
        public override float GetValue() => Mathf.Max(0, increaseCost ? base.GetValue() + cost() : base.GetValue() - cost());
    }
}
