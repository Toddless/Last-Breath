namespace Core.Interfaces.Battle.Decorator
{
    using Enums;
    using System;
    using Module;

    public abstract class StatModuleDecorator(EntityParameter abilityParameter, DecoratorPriority priority, string id)
        : IParameterModule<EntityParameter>, IModuleDecorator<EntityParameter, IParameterModule<EntityParameter>>
    {
        private IParameterModule<EntityParameter>? _module;

        public string Id { get; } = id;
        public EntityParameter Parameter { get; } = abilityParameter;
        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(IParameterModule<EntityParameter> module) => _module = module;

        public virtual float GetValue()
        {
            ArgumentNullException.ThrowIfNull(_module);
            return _module.GetValue();
        }

        public virtual float ApplyDecorators(float value)
        {
            ArgumentNullException.ThrowIfNull(_module);
            return _module.ApplyDecorators(value);
        }
    }
}
