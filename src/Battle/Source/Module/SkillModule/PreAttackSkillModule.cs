namespace Battle.Source.Module.SkillModule
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class PreAttackSkillModule(IFightable owner) : BaseSkillModule(owner, SkillType.BeforeAttack, Priority.Base)
    {
    }
}
