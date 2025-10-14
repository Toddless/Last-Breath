namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
    using Core.Interfaces;
    using System.Collections.Generic;

    public partial class HoverableItem : Panel
    {
        private HashSet<IModifier> _modifiers = [];
        private Func<HashSet<IModifier>>? _getModifiers;

        public override void _Ready()
        {
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            CustomMinimumSize = new Vector2(25, 25);
        }

        public override GodotObject _MakeCustomTooltip(string forText)
        {
            var popupWindow = PopupWindow.Initialize().Instantiate<PopupWindow>();
            var modifiers = _getModifiers?.Invoke();

            foreach (var mod in _modifiers.Concat(modifiers ?? []))
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

        public void SetModifiersToShow(HashSet<IModifier> modifiers) => _modifiers = modifiers;

        public void SetFuncToUpdateModifiers(Func<HashSet<IModifier>> getModifiers) => _getModifiers = getModifiers;
    }
}
