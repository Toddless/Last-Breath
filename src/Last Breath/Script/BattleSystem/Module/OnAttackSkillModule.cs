namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using LastBreath.Script;

    public class OnAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.OnAttack, DecoratorPriority.Base)
    {
    }
}
