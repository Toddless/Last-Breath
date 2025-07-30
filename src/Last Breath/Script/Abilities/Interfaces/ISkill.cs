namespace LastBreath.Script.Abilities.Interfaces
{
    using Godot;
    using LastBreath.Script.Enums;
    using LastBreath.Localization;

    public interface ISkill
    {
        string Id { get; }
        SkillType Type { get; }
        bool IsEvadable { get; }
        LocalizedString? Description { get; }
        Texture2D? Icon { get; }
    }
}
