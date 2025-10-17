namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Core.Interfaces.UI;

    [Tool]
    public partial class SkillDescription : Control, IInitializable, IClosable
    {
        private const string UID = "uid://swgtsw77jhvp";
        [Export] private Label? _name;
        [Export] private RichTextLabel? _description;

        public event Action? Close;

        public void SetSkillName(string text) => _name.Text = text;

        public void SetSkillDescription(string text) => _description.Text = text;

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void _ExitTree() => Close?.Invoke();
    }
}
