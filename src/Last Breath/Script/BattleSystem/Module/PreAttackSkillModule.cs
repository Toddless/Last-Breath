namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces;

    public class PreAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.PreAttack, DecoratorPriority.Base)
    {
    }
}
