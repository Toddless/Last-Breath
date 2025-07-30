namespace LastBreath.Script.BattleSystem.Module
{
    using LastBreath.Script;
    using LastBreath.Script.Enums;

    public class PreAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.PreAttack, DecoratorPriority.Base)
    {
    }
}
