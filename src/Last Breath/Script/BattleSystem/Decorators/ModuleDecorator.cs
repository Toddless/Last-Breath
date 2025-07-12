namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class ModuleDecorator : IModule
    {
        private IModule? _module;

        public ModuleParameter Parameter { get; private set; }

        public DecoratorPriority Priority { get; private set; }

        protected ModuleDecorator(ModuleParameter moduleParameter, DecoratorPriority priority)
        {
            Parameter = moduleParameter;
            Priority = priority;
        }

        public void ChainModule(IModule module) => _module = module;

        public virtual float GetValue() => _module?.GetValue() ?? 0;
    }
}
