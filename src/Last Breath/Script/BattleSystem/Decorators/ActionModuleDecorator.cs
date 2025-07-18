namespace Playground.Script.BattleSystem.Decorators
{
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class ActionModuleDecorator : IActionModule<ICharacter>, IModuleDecorator<ActionModule, IActionModule<ICharacter>>
    {
        private IActionModule<ICharacter>? _module;

        public ActionModuleDecorator(ActionModule type, DecoratorPriority priority)
        {
            Type = type;
            Priority = priority;
        }

        public ActionModule Type { get; }

        public DecoratorPriority Priority { get; }

        public void ChainModule(IActionModule<ICharacter> module) => _module = module;

        public virtual void PerformModuleAction(ICharacter target)
        {
            if (target.IsAlive) _module?.PerformModuleAction(target);
        }
    }
}
