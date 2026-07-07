namespace Battle.Source.Module.SkillModule
{
    using Core.Entity;
    using Core.Enums;

    public class GettingAttackSkillModule(IFightable owner) : BaseSkillModule(owner, type: SkillType.GettingAttack, priority:Priority.Base)
    {
    }
}
