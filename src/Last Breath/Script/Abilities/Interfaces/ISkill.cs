namespace Playground.Script.Abilities.Interfaces
{
    using Playground.Script.Enums;

    public interface ISkill
    {
        SkillType Type { get; }
        bool IsEvadable { get; }
    }
}
