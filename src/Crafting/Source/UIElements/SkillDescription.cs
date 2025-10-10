namespace Crafting.Source.UIElements
{
    using Godot;

    [Tool]
    public partial class SkillDescription : Control
    {
        private const string UID = "uid://swgtsw77jhvp";
        [Export] private Label? _name;
        [Export] private RichTextLabel? _description;

        public void SetSkillName(string text) => _name.Text = text;

        public void SetSkillDescription(string text) => _description.Text = text;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

    }
}
