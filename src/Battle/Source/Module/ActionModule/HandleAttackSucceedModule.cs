namespace Battle.Source.Module.ActionModule
{
    using Core.Enums;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;

    public class HandleAttackSucceedModule(IFightable owner) : IActionModule<IFightable>
    {
        private readonly IFightable _owner = owner;
        public ActionModule Parameter => ActionModule.SucceedAction;

        public Priority Priority => Priority.Base;

        public void PerformModuleAction(IFightable target)
        {
        }
    }
}
