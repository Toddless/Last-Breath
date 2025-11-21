namespace Battle.TestData.Abilities.Decorators
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    public abstract class AbilityParameterDecorator(AbilityParameter abilityParameter, DecoratorPriority priority, string id)
        : IParameterModule<AbilityParameter>, IModuleDecorator<AbilityParameter, IParameterModule<AbilityParameter>>
    {
        private IParameterModule<AbilityParameter>? _decorated;
        public string Id { get; } = id;
        public AbilityParameter Parameter { get; } = abilityParameter;
        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(IParameterModule<AbilityParameter> inner) => _decorated = inner;

        /// <summary>
        /// Get value from inner module.
        ///
        /// Throws <see cref="NullReferenceException"/> if inner module is null
        /// </summary>
        /// <returns></returns>
        public virtual float GetValue()
        {
            ArgumentNullException.ThrowIfNull(_decorated);
            return _decorated.GetValue();
        }

        public virtual float ApplyDecorators(float value)
        {
            ArgumentNullException.ThrowIfNull(_decorated);
            return _decorated.ApplyDecorators(value);
        }
    }
}
