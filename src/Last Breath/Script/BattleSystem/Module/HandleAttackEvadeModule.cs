namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class HandleAttackEvadeModule(ICharacter owner) : IActionModule<ICharacter>
    {
        private readonly ICharacter _owner = owner;
        public ActionModuleType ModuleType => ActionModuleType.EvadeAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(ICharacter parameter)
        {
         
        }
    }
}
