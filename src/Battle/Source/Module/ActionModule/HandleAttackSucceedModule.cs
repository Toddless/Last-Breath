namespace Battle.Source.Module.ActionModule
{
    using Core.Components.Module;
    using Core.Entity;
    using Core.Enums;

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
