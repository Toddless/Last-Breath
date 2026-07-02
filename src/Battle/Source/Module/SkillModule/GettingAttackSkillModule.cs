namespace Battle.Source.Module.SkillModule
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class GettingAttackSkillModule(IFightable owner) : BaseSkillModule(owner, type: SkillType.GettingAttack, priority:Priority.Base)
    {
    }
}
