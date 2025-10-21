namespace Crafting.Source.UIElements
{
    using Godot;

    [GlobalClass]
    public partial class HoverableSkill : PanelContainer
    {
        private RichTextLabel? _skillDescription;
        public override void _Ready()
        {
            _skillDescription = new()
            {
                FitContent = true,
                CustomMinimumSize = new Vector2(25, 25)
            };
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            CustomMinimumSize = new Vector2(25, 25);
        }

        public override GodotObject _MakeCustomTooltip(string forText)
        {
            var popup = PopupWindow.Initialize().Instantiate<PopupWindow>();
            popup.AddChild(_skillDescription);
            return popup;
        }

        public void SetSkillName(Label label) => AddChild(label);
        public void SetSkillDescription(string Label) => _skillDescription.Text = Label;
    }
}
