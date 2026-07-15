namespace Core.Entity.Components.Decorator
{
    using Enums;
    using Module;

    public class EntityParameterDecorator(
        string id,
        float value,
        OperationType operation,
        EntityParameter parameter,
        Priority priority)
        : EntityParameterModuleDecorator(parameter, priority, id)
    {
        public float Value { get; } = value;
        public override float GetValue() => operation switch
        {
            OperationType.Add => base.GetValue() + Value,
            OperationType.Divide => base.GetValue() / Value,
            OperationType.Multiply => base.GetValue() * Value,
            OperationType.Subtract => base.GetValue() - Value,
            OperationType.Override => Value,
            _ => base.GetValue()
        };

        public override bool IsStronger(IModuleDecorator<EntityParameter, IParameterModule<EntityParameter>> decorator)
        {
            if (decorator is not EntityParameterDecorator parameterDecorator) return false;
            return parameterDecorator.Id == Id && Value > parameterDecorator.Value;
        }
    }
}
