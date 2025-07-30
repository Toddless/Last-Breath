namespace LastBreath.Script.BattleSystem.Module
{
    using LastBreath.Script;
    using LastBreath.Script.Enums;

    public class AlwaysActiveSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  DecoratorPriority.Base)
    {
    }
}
