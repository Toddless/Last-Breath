namespace Battle.Source.Module.ActionModule
{
    using Core.Enums;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;

    public class HandleAttackBlockedModule(IEntity owner) : IActionModule<IEntity>
    {
        private readonly IEntity _owner = owner;
        public ActionModule Parameter => ActionModule.BlockAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(IEntity target)
        {
        }
    }
}
