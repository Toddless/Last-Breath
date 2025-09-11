namespace Core.Interfaces.Skills
{
    using Core.Enums;

    public interface ISkill : IIdentifiable,  IDisplayable
    {
        SkillType Type { get; }
        bool IsEvadable { get; }
    }
}
