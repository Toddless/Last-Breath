namespace LastBreath.Script.BattleSystem.Module
{
    using LastBreath.Script;
    using LastBreath.Script.Enums;

    public class GettingAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, type: SkillType.GettingAttack, priority:DecoratorPriority.Base)
    {
    }
}
