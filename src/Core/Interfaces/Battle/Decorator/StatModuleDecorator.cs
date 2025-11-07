namespace Core.Interfaces.Battle.Decorator
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;

    public abstract class StatModuleDecorator(Parameter parameter, DecoratorPriority priority, string id) : IParameterModule, IModuleDecorator<Parameter, IParameterModule>
    {
        private IParameterModule? _module;

        public Parameter Parameter { get; } = parameter;
        public DecoratorPriority Priority { get; } = priority;
        public string Id { get; } = id;

        public void ChainModule(IParameterModule module) => _module = module;

        public virtual float GetValue() => _module?.GetValue() ?? 0;
    }
}
