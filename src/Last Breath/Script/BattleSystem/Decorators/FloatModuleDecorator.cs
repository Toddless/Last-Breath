namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class FloatModuleDecorator(ModuleParameter moduleParameter, DecoratorPriority priority) : IValueModule<float>
    {
        private IValueModule<float>? _module;

        public ModuleParameter Parameter { get; } = moduleParameter;

        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(IValueModule<float> module) => _module = module;

        public virtual float GetValue() => _module?.GetValue() ?? 0;
    }
}
