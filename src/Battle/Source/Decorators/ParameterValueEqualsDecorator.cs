namespace Battle.Source.Decorators
{
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;

    public class ParameterValueEqualsDecorator(
        float value,
        EntityParameter parameter,
        string id)
        : EntityParameterModuleDecorator(parameter, priority: DecoratorPriority.Absolute, id)
    {
        public float Value { get; } = value;

        public override float GetValue()
        {
            base.GetValue();
            return Value;
        }

        public override float ApplyDecoratorsForValue(float applyToValue)
        {
            base.ApplyDecoratorsForValue(applyToValue);
            return Value;
        }
    }
}
