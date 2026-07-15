namespace Core.Entity.Components.Decorator
{
    using System;
    using Enums;
    using Module;

    public abstract class EntityParameterModuleDecorator(EntityParameter parameter, Priority priority, string id)
        : IParameterModule<EntityParameter>, IModuleDecorator<EntityParameter, IParameterModule<EntityParameter>>
    {
        private IParameterModule<EntityParameter>? _module;

        public string Id { get; } = id;
        public EntityParameter Parameter { get; } = parameter;
        public Priority Priority { get; } = priority;


        public void ChainModule(IParameterModule<EntityParameter> module) => _module = module;


        public virtual float GetValue()
        {
            ArgumentNullException.ThrowIfNull(_module);
            return _module.GetValue();
        }

        public virtual float ApplyDecoratorsForValue(float applyToValue)
        {
            ArgumentNullException.ThrowIfNull(_module);
            return _module.ApplyDecoratorsForValue(applyToValue);
        }

        public virtual bool IsStronger(IModuleDecorator<EntityParameter, IParameterModule<EntityParameter>> decorator) => Id == decorator.Id;
    }
}
