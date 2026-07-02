namespace Battle.Source.Module.ActionModule
{
    using Core.Enums;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;

    public class HandleAttackBlockedModule(IFightable owner) : IActionModule<IFightable>
    {
        private readonly IFightable _owner = owner;
        public ActionModule Parameter => ActionModule.BlockAction;

        public Priority Priority => Priority.Base;

        public void PerformModuleAction(IFightable target)
        {
        }
    }
}
