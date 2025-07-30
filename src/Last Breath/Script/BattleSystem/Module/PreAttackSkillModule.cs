namespace Playground.Script.BattleSystem.Module
{
    using Playground.Script.Enums;

    public class PreAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.PreAttack, DecoratorPriority.Base)
    {
    }
}
