namespace Core.Interfaces.Skills
{
    using Core.Enums;

    public interface ISkill
    {
        string Id { get; }
        SkillType Type { get; }
        bool IsEvadable { get; }
    }
}
