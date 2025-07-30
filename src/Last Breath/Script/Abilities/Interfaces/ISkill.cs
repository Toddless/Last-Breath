namespace Playground.Script.Abilities.Interfaces
{
    using Godot;
    using Playground.Localization;
    using Playground.Script.Enums;

    public interface ISkill
    {
        string Id { get; }
        SkillType Type { get; }
        bool IsEvadable { get; }
        LocalizedString? Description { get; }
        Texture2D? Icon { get; }
    }
}
