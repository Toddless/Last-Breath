namespace Battle.Source.Abilities.Decorators
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;

    public abstract class AbilityParameterDecorator(AbilityParameter abilityParameter, DecoratorPriority priority, string id)
        : IParameterModule<AbilityParameter>, IModuleDecorator<AbilityParameter, IParameterModule<AbilityParameter>>
    {
        private IParameterModule<AbilityParameter>? _decorated;
        public string Id { get; } = id;
        public AbilityParameter Parameter { get; } = abilityParameter;
        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(IParameterModule<AbilityParameter> inner) => _decorated = inner;

        public virtual float GetValue()
        {
            ArgumentNullException.ThrowIfNull(_decorated);
            return _decorated.GetValue();
        }

        public virtual float ApplyDecoratorsForValue(float value)
        {
            ArgumentNullException.ThrowIfNull(_decorated);
            return _decorated.ApplyDecoratorsForValue(value);
        }
    }
}
