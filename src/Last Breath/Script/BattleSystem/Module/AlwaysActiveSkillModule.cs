namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class AlwaysActiveSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  DecoratorPriority.Base)
    {
    }
}
