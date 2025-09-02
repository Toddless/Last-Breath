namespace LastBreath.Script.BattleSystem.Module
{
    using Core.Enums;
    using Core.Interfaces;

    public class OnAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.OnAttack, DecoratorPriority.Base)
    {
    }
}
