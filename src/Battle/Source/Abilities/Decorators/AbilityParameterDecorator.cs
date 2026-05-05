namespace Battle.Source.Abilities.Decorators
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Components.Decorator;

    public abstract class AbilityParameterDecorator<TParameter>(TParameter abilityParameter, DecoratorPriority priority, string id)
        : IParameterModule<TParameter>, IModuleDecorator<TParameter, IParameterModule<TParameter>>
        where TParameter : struct, Enum
    {
        private IParameterModule<TParameter>? _decorated;
        public string Id { get; } = id;
        public TParameter Parameter { get; } = abilityParameter;
        public DecoratorPriority Priority { get; } = priority;

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
    }
}
