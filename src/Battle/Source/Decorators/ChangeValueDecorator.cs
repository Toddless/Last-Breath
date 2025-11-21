namespace Battle.Source.Decorators
{
    using Core.Enums;
    using Core.Interfaces.Battle.Decorator;

    public class ChangeValueDecorator(EntityParameter abilityParameter, DecoratorPriority priority, string id, float value) : StatModuleDecorator(abilityParameter, priority, id)
    {
        public override float GetValue() => base.GetValue() * value;
        public override float ApplyDecorators(float baseValue) => base.ApplyDecorators(baseValue) * value;
    }
}
