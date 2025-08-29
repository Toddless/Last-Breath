namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using LastBreath.Script;

    public class HandleAttackBlockedModule(ICharacter owner) : IActionModule<ICharacter>
    {
        private readonly ICharacter _owner = owner;
        public ActionModule SkillType => ActionModule.BlockAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(ICharacter target)
        {
            _owner.CurrentStance?.Resource.Recover();
        }
    }
}
