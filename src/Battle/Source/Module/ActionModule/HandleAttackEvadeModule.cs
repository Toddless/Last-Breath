namespace Battle.Source.Module.ActionModule
{
    using Core.Enums;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;

    public class HandleAttackEvadeModule(IEntity owner) : IActionModule<IEntity>
    {
        private readonly IEntity _owner = owner;
        public ActionModule Parameter => ActionModule.EvadeAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(IEntity target)
        {

        }
    }
}
