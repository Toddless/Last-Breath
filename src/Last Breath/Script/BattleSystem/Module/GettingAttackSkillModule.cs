namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class GettingAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, type: SkillType.GettingAttack, priority:DecoratorPriority.Base)
    {
    }
}
