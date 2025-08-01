namespace LastBreath.Script.BattleSystem.Module
{
    using Contracts.Enums;
    using LastBreath.Script;

    public class OnAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.OnAttack, DecoratorPriority.Base)
    {
    }
}
