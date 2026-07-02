namespace Battle.Source.Module.SkillModule
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class OnAttackSkillModule(IFightable owner) : BaseSkillModule(owner, SkillType.AfterAttack, Priority.Base)
    {
    }
}
