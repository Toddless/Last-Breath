namespace Playground.Script.Abilities.Skills
{
    using Godot;
    using Playground.Script.Enums;

    public interface ISkill
    {
        string Name { get; }
        string Description { get; }
        Texture2D? Icon { get; }
        SkillType SkillType { get; }
        public void OnObtaining(ICharacter owner);
        public void OnLoss();
    }
}
