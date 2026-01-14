namespace Battle.Source.Abilities.Decorators
{
    using System;
    using Core.Enums;
    using Godot;

    public class AbilityParameterChangeTargetsDecorator(Func<float> targets, bool increase = false)
        : AbilityParameterDecorator(abilityParameter: AbilityParameter.Target, priority: DecoratorPriority.Strong, id: "Ability_Parameter_Change_Targets_Decorator")
    {
        public override float GetValue() => Mathf.Max(0, increase ? base.GetValue() + targets() : base.GetValue() - targets());
    }
}
