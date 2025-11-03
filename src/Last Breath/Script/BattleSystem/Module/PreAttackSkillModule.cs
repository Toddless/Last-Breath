namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class PreAttackSkillModule(IEntity owner) : BaseSkillModule(owner, SkillType.PreAttack, DecoratorPriority.Base)
    {
    }
}
