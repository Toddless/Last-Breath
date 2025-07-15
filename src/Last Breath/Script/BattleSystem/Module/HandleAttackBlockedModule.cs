namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class HandleAttackBlockedModule(ICharacter owner) : IActionModule<ICharacter>
    {
        private readonly ICharacter _owner = owner;
        public ActionModuleType ModuleType => ActionModuleType.BlockAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(ICharacter target)
        {

        }
    }
}
