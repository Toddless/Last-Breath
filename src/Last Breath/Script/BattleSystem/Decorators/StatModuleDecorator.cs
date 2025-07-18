namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class StatModuleDecorator : IStatModule
    {
        private IStatModule? _module;

        public StatModuleDecorator(StatModule statModule, DecoratorPriority priority)
        {
            ModuleType = statModule;
            Priority = priority;
        }

        public StatModule ModuleType { get; }

        public DecoratorPriority Priority { get; }

        public void ChainModule(IStatModule module) => _module = module;

        public virtual float GetValue() => _module?.GetValue() ?? 0;
    }
}
