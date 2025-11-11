namespace Battle.Source.Module.SkillModule
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class GettingAttackSkillModule(IEntity owner) : BaseSkillModule(owner, type: SkillType.GettingAttack, priority:DecoratorPriority.Base)
    {
    }
}
