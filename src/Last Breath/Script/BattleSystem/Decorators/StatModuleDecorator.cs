namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class StatModuleDecorator : IStatModule, IModuleDecorator<StatModule, IStatModule>
    {
        private IStatModule? _module;

        public StatModuleDecorator(StatModule statModule, DecoratorPriority priority)
        {
            Type = statModule;
            Priority = priority;
        }

        public StatModule Type { get; }

        public DecoratorPriority Priority { get; }

        public void ChainModule(IStatModule module) => _module = module;

        public virtual float GetValue() => _module?.GetValue() ?? 0;
    }
}
