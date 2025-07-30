namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class OnAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.OnAttack, DecoratorPriority.Base)
    {
    }
}
