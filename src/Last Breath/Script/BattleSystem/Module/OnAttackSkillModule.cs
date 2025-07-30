namespace LastBreath.Script.BattleSystem.Module
{
    using LastBreath.Script;
    using LastBreath.Script.Enums;

    public class OnAttackSkillModule(ICharacter owner) : BaseSkillModule(owner, SkillType.OnAttack, DecoratorPriority.Base)
    {
    }
}
