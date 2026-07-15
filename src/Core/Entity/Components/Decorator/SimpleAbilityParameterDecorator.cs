namespace Core.Entity.Components.Decorator
{
    using System;
    using Enums;

    public class SimpleAbilityParameterDecorator<TParameter>(TParameter abilityParameter, Priority priority, OperationType type, float value, string id,string source)
        : AbilityParameterDecorator<TParameter>(abilityParameter, priority, id, source)
        where TParameter : struct, Enum
    {
        public override float GetValue() => PerformCalculation(base.GetValue());

        private float PerformCalculation(float baseValue) => type switch
        {
            OperationType.Add => baseValue + value,
            OperationType.Subtract => baseValue - value,
            OperationType.Divide => baseValue / value,
            OperationType.Multiply => baseValue * value,
            OperationType.Override => value,
            _ => baseValue
        };
    }
}
