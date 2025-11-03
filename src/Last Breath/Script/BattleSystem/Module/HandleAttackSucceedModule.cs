namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Entity;

    public class HandleAttackSucceedModule(IEntity owner) : IActionModule<IEntity>
    {
        private readonly IEntity _owner = owner;
        public ActionModule SkillType => ActionModule.SucceedAction;

        public DecoratorPriority Priority => DecoratorPriority.Base;

        public void PerformModuleAction(IEntity target)
        {
        }
    }
}
