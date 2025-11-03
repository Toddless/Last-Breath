namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class OnAttackSkillModule(IEntity owner) : BaseSkillModule(owner, SkillType.OnAttack, DecoratorPriority.Base)
    {
    }
}
