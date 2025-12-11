namespace Battle.Source.Abilities.Decorators
{
    using System;
    using Core.Enums;
    using Godot;

    public class AbilityParameterChangeDamageDecorator( Func<float> getValue, bool increase = false)
        : AbilityParameterDecorator(abilityParameter: AbilityParameter.Damage, priority: DecoratorPriority.Weak, id: "Ability_Parameter_Change_Damage_Decorator")
    {
        public override float GetValue() => Mathf.Max(0, increase ? base.GetValue() + getValue() : base.GetValue() - getValue());
    }
}
