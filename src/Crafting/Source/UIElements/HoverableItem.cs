namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
    using System.Collections.Generic;
    using Core.Modifiers;

    public partial class HoverableItem : Panel
    {
        private IEnumerable<IModifier> _modifiers = [];
        private Func<IEnumerable<IModifier>>? _getModifiers;

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
            var allModifiers = _modifiers.Concat(modifiers ?? []);
            var sorted = allModifiers.GroupBy(mod => mod.GetHashCode()).Select(x => x.OrderByDescending(mod => mod.BaseValue).First());
            foreach (var mod in sorted)
            {
                var label = new Label
                {
                    LabelSettings = new()
                    {
                        FontSize = 12
                    },
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Text = Localization.Format(mod)
                };
                popupWindow.AddItem(label);
            }
            return popupWindow;
        }

        public void SetModifiersToShow(HashSet<IModifier> modifiers) => _modifiers = modifiers;

        public void SetFuncToUpdateModifiers(Func<IEnumerable<IModifier>> getModifiers) => _getModifiers = getModifiers;
    }
}
