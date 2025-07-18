namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class ActionModuleDecorator(ActionModule type, DecoratorPriority priority) : IActionModule<ICharacter>
    {
        private IActionModule<ICharacter>? _module;

        public ActionModule ModuleType { get; } = type;

        public DecoratorPriority Priority { get; } = priority;

        public void ChainModule(IActionModule<ICharacter> module) => _module = module;

        public virtual void PerformModuleAction(ICharacter target)
        {
            if (target.IsAlive) _module?.PerformModuleAction(target);
        }
    }
}
