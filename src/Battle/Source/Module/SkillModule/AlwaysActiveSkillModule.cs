namespace Battle.Source.Module.SkillModule
{
    using Core.Enums;
    using Core.Interfaces.Entity;

    public class AlwaysActiveSkillModule(IFightable owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  Priority.Base)
    {
    }
}
