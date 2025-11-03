namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class AlwaysActiveSkillModule(IEntity owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  DecoratorPriority.Base)
    {
    }
}
