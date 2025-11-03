namespace Crafting.Source.UIElements
{
    using Godot;
    using Core.Interfaces.UI;

    public partial class SkillDescription : Control, IInitializable
    {
        private const string UID = "uid://swgtsw77jhvp";
        [Export] private Label? _name;
        [Export] private RichTextLabel? _description;

        public void SetSkillName(string text) => _name.Text = text;

        public void SetSkillDescription(string text) => _description.Text = text;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
