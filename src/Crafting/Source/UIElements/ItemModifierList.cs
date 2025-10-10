namespace Crafting.Source.UIElements
{
    using Godot;
    using System.Linq;
    using System.Collections.Generic;

    [GlobalClass]
    public partial class ItemModifierList : Control
    {
        private const string UID = "uid://b6glmp15vrdpg";
        private int _lastSelectedChild = -1;
        [Export] private VBoxContainer? _container;

        [Signal] public delegate void ItemSelectedEventHandler(int hash, ItemModifierList source);

        public void AddModifiersToList(List<(string Mod, int Hash)> modifiers)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                var item = new SelectableItem();
                item.SetIndex(i);
                item.SetText(modifiers[i].Mod);
                item.SetMetadata(modifiers[i].Hash);
                item.SetSelectable(false);
                item.Selected += OnItemSelected;
                _container?.AddChild(item);
            }
        }

        public void SetItemSelectable(bool selectable = true)
        {
            foreach (var item in _container?.GetChildren().Where(child => child is SelectableItem).Cast<SelectableItem>() ?? [])
                item.SetSelectable(selectable);
        }

        public void UpdateSelectedItem((string Mod, int Hash) newModifier)
        {
            var selectableItem = _container?.GetChild<SelectableItem>(_lastSelectedChild);
            selectableItem?.SetText(newModifier.Mod);
            selectableItem?.SetMetadata(newModifier.Hash);
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnItemSelected(int index)
        {
            _lastSelectedChild = index;
            var hash = _container?.GetChild<SelectableItem>(index).GetMetadata().AsInt32() ?? 0;
            if (hash != 0)
            {
                EmitSignal(SignalName.ItemSelected, hash, this);
            }
        }
    }
}
