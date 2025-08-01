namespace LastBreath.Script.Abilities.Interfaces
{
    using Godot;
    using LastBreath.Localization;
    using Contracts.Enums;

    public interface ISkill
    {
        string Id { get; }
        SkillType Type { get; }
        bool IsEvadable { get; }
        LocalizedString? Description { get; }
        Texture2D? Icon { get; }
    }
}
