namespace Battle.Source.Module.SkillModule
{
    using Core.Entity;
    using Core.Enums;

    public class PreAttackSkillModule(IFightable owner) : BaseSkillModule(owner, SkillType.BeforeAttack, Priority.Base)
    {
    }
}
