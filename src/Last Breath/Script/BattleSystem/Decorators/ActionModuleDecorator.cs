namespace Playground.Script.BattleSystem.Decorators
{
    using System;
    using Playground.Script.BattleSystem.Module;
    using Playground.Script.Enums;

    public abstract class ActionModuleDecorator : IActionModule<ICharacter>, IModuleDecorator<ActionModule, IActionModule<ICharacter>>
    {
        private IActionModule<ICharacter>? _module;
        private readonly Lazy<string> _id;

        public ActionModule SkillType { get; }
        public DecoratorPriority Priority { get; }
        public string Id => _id.Value;

        public ActionModuleDecorator(ActionModule type, DecoratorPriority priority)
        {
            SkillType = type;
            Priority = priority;
            _id = new(CreateID);
        }

        public void ChainModule(IActionModule<ICharacter> module) => _module = module;

        public virtual void PerformModuleAction(ICharacter target)
        {
            if (target.IsAlive) _module?.PerformModuleAction(target);
        }

        protected virtual string CreateID() => $"{GetType().Name}_{SkillType}_{Priority}";
    }
}
