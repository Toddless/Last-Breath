namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using LastBreath.Script;

    public class PreAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.PreAttack, DecoratorPriority.Base)
    {
    }
}
