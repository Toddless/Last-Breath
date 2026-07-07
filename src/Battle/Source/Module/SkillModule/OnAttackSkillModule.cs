namespace Battle.Source.Module.SkillModule
{
    using Core.Entity;
    using Core.Enums;

    public class OnAttackSkillModule(IFightable owner) : BaseSkillModule(owner, SkillType.AfterAttack, Priority.Base)
    {
    }
}
