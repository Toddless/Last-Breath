namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces;

    public class AlwaysActiveSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  DecoratorPriority.Base)
    {
    }
}
