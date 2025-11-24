namespace Battle.Source.Decorators
{
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class ChangeValueDecorator(EntityParameter parameter, DecoratorPriority priority, string id, float value) : EntityParameterModuleDecorator(parameter, priority, id)
    {
        public override float GetValue() => base.GetValue() * value;
        public override float ApplyDecoratorsForValue(float baseValue) => base.ApplyDecoratorsForValue(baseValue) * value;
    }
}
