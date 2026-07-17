namespace Core.Battle.Abilities
{
    using Enums;

    public class SimpleAbilityParameterDecorator(string parameter, Priority priority, OperationType type, float value, string id, string source)
        : AbilityParameterDecorator(parameter, priority, id, source)
    {
        public override float Decorate(float baseValue) => type switch
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
