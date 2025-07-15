namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class ActionModuleDecorator(ActionModuleType type, DecoratorPriority priority) : IActionModule<ICharacter>
    {
        private IActionModule<ICharacter>? _module;

        public ActionModuleType ModuleType { get; } = type;

        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(IActionModule<ICharacter> module) => _module = module;

        public virtual void PerformModuleAction(ICharacter target)
        {
            if (target.IsAlive) _module?.PerformModuleAction(target);
        }
    }
}
