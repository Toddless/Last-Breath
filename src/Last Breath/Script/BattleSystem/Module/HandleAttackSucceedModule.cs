namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using LastBreath.Script;

    public class HandleAttackSucceedModule(ICharacter owner) : IActionModule<ICharacter>
    {
        private readonly ICharacter _owner = owner;
        public ActionModule SkillType => ActionModule.SucceedAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(ICharacter target)
        {
            _owner.CurrentStance?.Resource.Recover();
        }
    }
}
