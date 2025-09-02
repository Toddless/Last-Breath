namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces;

    public class GettingAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, type: SkillType.GettingAttack, priority:DecoratorPriority.Base)
    {
    }
}
