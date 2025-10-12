namespace Crafting.Source.UIElements
{
    using Godot;

    public partial class PopupWindow : PanelContainer
    {
        private BoxContainer? _childContainer;

        public void Setup()
        {
            this.CustomMinimumSize = new Vector2(100, 150);
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_top", 15);
            margin.AddThemeConstantOverride("margin_left", 15);
            margin.AddThemeConstantOverride("margin_right", 15);
            margin.AddThemeConstantOverride("margin_bottom", 15);
            _childContainer = new VBoxContainer
            {
                Alignment = BoxContainer.AlignmentMode.Center
            };
            margin.AddChild(_childContainer);
            AddChild(margin);
        }

        public void AddItem(Node item) => _childContainer?.AddChild(item);
    }
}
