namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using LastBreath.Script;

    public class AlwaysActiveSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  DecoratorPriority.Base)
    {
    }
}
