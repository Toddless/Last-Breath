namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using System.Linq;
    using Core.Interfaces.UI;
    using System.Collections.Generic;

    [GlobalClass]
    public partial class ItemModifierList : Control, IInitializable
    {
        private const string UID = "uid://b6glmp15vrdpg";
        private int _lastSelectedChild = -1;
        [Export] private VBoxContainer? _container;
        private readonly Dictionary<int, InteractiveLabel> _labels = [];

        [Signal] public delegate void ItemSelectedEventHandler(int hash, ItemModifierList source);

        public void AddModifiersToList(List<(string Mod, int Hash)> modifiers, LabelSettings? labelSettings)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                var item = new InteractiveLabel();
                item.SetIndex(i);
                item.SetText(modifiers[i].Mod);
                item.SetMetadata(modifiers[i].Hash);
                item.SetSelectable(false);
                item.SetLabelSetting(labelSettings);
                item.Selected += OnItemSelected;
                _container?.AddChild(item);
                _labels[modifiers[i].Hash] = item;
            }
        }

        public void SetItemsSelectable(bool selectable = true)
        {
            foreach (var item in _container?.GetChildren().Cast<InteractiveLabel>() ?? [])
                item.SetSelectable(selectable);
        }

        public void UpdateModifierText(int hash, string newText)
        {
            if (_labels.TryGetValue(hash, out var label))
                label.UpdateText(newText);
        }

        public void UpdateSelectedItem((string Mod, int Hash) newModifier)
        {
            ArgumentNullException.ThrowIfNull(_container);
            var selectableItem = _container.GetChild<InteractiveLabel>(_lastSelectedChild);
            var oldHash = selectableItem.GetMetadata().AsInt32();
            selectableItem.SetText(newModifier.Mod);
            selectableItem.SetMetadata(newModifier.Hash);
            if( _labels.TryGetValue(oldHash, out var label))
            {
                _labels.Remove(_lastSelectedChild);
                _labels[newModifier.Hash] = label;
            }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnItemSelected(int index)
        {
            _lastSelectedChild = index;
            var hash = _container?.GetChild<InteractiveLabel>(index).GetMetadata().AsInt32() ?? 0;
            if (hash != 0) EmitSignal(SignalName.ItemSelected, hash, this);
        }
    }
}
