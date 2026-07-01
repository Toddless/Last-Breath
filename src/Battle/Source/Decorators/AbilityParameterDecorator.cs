namespace Battle.Source.Decorators
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;

    public abstract class AbilityParameterDecorator<TParameter>(TParameter abilityParameter, Priority priority, string id, string source)
        : IParameterModule<TParameter>, IModuleDecorator<TParameter, IParameterModule<TParameter>>
        where TParameter : struct, Enum
    {
        private IParameterModule<TParameter>? _decorated;
        public string Id { get; } = id;
        public string Source { get; } = source;
        public TParameter Parameter { get; } = abilityParameter;
        public Priority Priority { get; } = priority;

        public void ChainModule(IParameterModule<TParameter> inner) => _decorated = inner;

        public virtual float GetValue()
        {
            ArgumentNullException.ThrowIfNull(_decorated);
            return _decorated.GetValue();
        }

        public virtual float ApplyDecoratorsForValue(float applyToValue)
        {
            ArgumentNullException.ThrowIfNull(_decorated);
            return _decorated.ApplyDecoratorsForValue(applyToValue);
        }

        public virtual bool IsStronger(IModuleDecorator<TParameter, IParameterModule<TParameter>> decorator)=>decorator.Id == Id;
    }
}
