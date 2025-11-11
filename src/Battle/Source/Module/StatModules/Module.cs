namespace Battle.Source.Module.StatModules
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    public abstract class Module(Func<float> value, Parameter parameter) : IParameterModule
    {
        protected readonly Func<float> Value = value;
        public Parameter Parameter { get; } = parameter;
        public DecoratorPriority Priority { get; } = DecoratorPriority.Base;
        public virtual float GetValue() => Value.Invoke();
    }
}
