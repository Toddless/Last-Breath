namespace Crafting.Source.UIElements
{
    using Godot;
    using Utilities;
    using Core.Interfaces;
    using System.Collections.Generic;

    public partial class HoverableItem : Panel
    {
        private List<IModifier> _modifiers = [];

        public override void _Ready()
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            CustomMinimumSize = new Vector2(25, 25);
        }

        public override GodotObject _MakeCustomTooltip(string forText)
        {
            var popupWindow = PopupWindow.Initialize().Instantiate<PopupWindow>();

            foreach (var mod in _modifiers)
            {
                var label = new Label
                {
                    LabelSettings = new()
                    {
                        FontSize = 12
                    },
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = Lokalizator.Format(mod)
                };
                popupWindow.AddItem(label);
            }
            return popupWindow;
        }

        public void SetModifiersToShow(List<IModifier> modifiers) => _modifiers = modifiers;
    }
}
