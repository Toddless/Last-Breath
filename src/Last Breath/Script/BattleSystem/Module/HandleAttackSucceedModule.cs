namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class HandleAttackSucceedModule(ICharacter owner) : IActionModule<ICharacter>
    {
        private readonly ICharacter _owner = owner;
        public ActionModule ModuleType => ActionModule.SucceedAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(ICharacter parameter)
        {
            _owner.CurrentStance?.Resource.Recover();
        }
    }
}
