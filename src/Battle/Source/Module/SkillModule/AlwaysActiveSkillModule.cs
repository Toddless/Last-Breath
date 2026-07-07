namespace Battle.Source.Module.SkillModule
{
    using Core.Entity;
    using Core.Enums;

    public class AlwaysActiveSkillModule(IFightable owner) : BaseSkillModule(owner, SkillType.AlwaysActive,  Priority.Base)
    {
    }
}
