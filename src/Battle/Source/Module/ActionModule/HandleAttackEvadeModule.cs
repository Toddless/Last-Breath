namespace Battle.Source.Module.ActionModule
{
    using Core.Components.Module;
    using Core.Entity;
    using Core.Enums;

    public class HandleAttackEvadeModule(IFightable owner) : IActionModule<IFightable>
    {
        private readonly IFightable _owner = owner;
        public ActionModule Parameter => ActionModule.EvadeAction;

        public Priority Priority => Priority.Base;

        public void PerformModuleAction(IFightable target)
        {

        }
    }
}
