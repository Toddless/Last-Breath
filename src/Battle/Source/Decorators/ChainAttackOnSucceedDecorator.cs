namespace Battle.Source.Decorators
{
    using Core.Enums;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Entity;

    public class ChainAttackOnSucceedDecorator(DecoratorPriority priority, IEntity owner) : ActionModuleDecorator(type: ActionModule.SucceedAction, priority)
    {
        private readonly IEntity _owner = owner;

        public override void PerformModuleAction(IEntity target)
        {
            base.PerformModuleAction(target);
        }
    }
}
