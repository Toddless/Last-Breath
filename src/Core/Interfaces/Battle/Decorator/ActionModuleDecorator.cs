namespace Core.Interfaces.Battle.Decorator
{
    using System;
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Entity;

    public abstract class ActionModuleDecorator : IActionModule<IEntity>, IModuleDecorator<ActionModule, IActionModule<IEntity>>
    {
        private IActionModule<IEntity>? _module;
        private readonly Lazy<string> _id;

        public ActionModule Parameter { get; }
        public DecoratorPriority Priority { get; }
        public string Id => _id.Value;

        public ActionModuleDecorator(ActionModule type, DecoratorPriority priority)
        {
            Parameter = type;
            Priority = priority;
            _id = new(CreateID);
        }

        public void ChainModule(IActionModule<IEntity> module) => _module = module;

        public virtual void PerformModuleAction(IEntity target)
        {
            if (target.IsAlive) _module?.PerformModuleAction(target);
        }

        protected virtual string CreateID() => $"{GetType().Name}_{Parameter}_{Priority}";
    }
}
