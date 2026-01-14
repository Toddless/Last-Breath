namespace Core.Interfaces.Components.Decorator
{
    using System;
    using Enums;
    using Module;

    public abstract class EntityParameterModuleDecorator(EntityParameter parameter, DecoratorPriority priority, string id)
        : IParameterModule<EntityParameter>, IModuleDecorator<EntityParameter, IParameterModule<EntityParameter>>
    {
        private IParameterModule<EntityParameter>? _module;

        public string Id { get; } = $"{parameter}_{id}_{priority}";
        public EntityParameter Parameter { get; } = parameter;
        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(IParameterModule<EntityParameter> module) => _module = module;

        public virtual float GetValue()
        {
            ArgumentNullException.ThrowIfNull(_module);
            return _module.GetValue();
        }

        public virtual float ApplyDecoratorsForValue(float value)
        {
            ArgumentNullException.ThrowIfNull(_module);
            return _module.ApplyDecoratorsForValue(value);
        }
    }
}
