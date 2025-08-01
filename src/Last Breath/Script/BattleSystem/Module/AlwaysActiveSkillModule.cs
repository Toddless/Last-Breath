namespace LastBreath.Script.BattleSystem.Module
{
    using Contracts.Enums;
    using LastBreath.Script;

    public class AlwaysActiveSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  DecoratorPriority.Base)
    {
    }
}
