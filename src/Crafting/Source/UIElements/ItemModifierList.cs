namespace Crafting.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Views.UI;
    using Godot;
    using Godot.Collections;

    [Tool]
    [GlobalClass]
    public partial class ItemModifierList : VBoxContainer, IInitializable
    {
        private const string UID = "uid://b6glmp15vrdpg";
        private int _lastSelectedLabelIdentifier = -1;
        [Export] private Array<InteractiveLabel> _labels = [];

        [Signal]
        public delegate void ItemSelectedEventHandler(int identifier, ItemModifierList source);

        public void AddModifiersToList(List<(string ModifierText, int Identifier)> modifiers)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                var label = _labels[i];
                label.Identifier = modifiers[i].Identifier;
                label.SetText(modifiers[i].ModifierText);
                label.Selected += OnItemSelected;
            }
        }

        public void SetItemsSelectable(bool selectable = true)
        {
            foreach (var item in GetChildren().Cast<InteractiveLabel>())
                item.Selectable = selectable;
        }

        public void UpdateModifierText(int identifier, string newText) => _labels.FirstOrDefault(x => x.Identifier == identifier)?.SetText(newText);

        public void UpdateSelectedItem((string ModifierText, int Identifier) newModifier)
        {
            var selectableItem = _labels.FirstOrDefault(x => x.Identifier == _lastSelectedLabelIdentifier);
            selectableItem?.SetText(newModifier.ModifierText);
            selectableItem?.Identifier = newModifier.Identifier;
            _lastSelectedLabelIdentifier = -1;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnItemSelected(int identifier)
        {
            _lastSelectedLabelIdentifier = identifier;
            EmitSignal(SignalName.ItemSelected, _lastSelectedLabelIdentifier, this);
        }
    }
}
