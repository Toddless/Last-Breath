namespace Crafting.Source.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    [GlobalClass]
    public partial class ItemModifierList : Control
    {
        private int _lastSelectedChild = -1;
        [Export] private VBoxContainer? _container;

        [Signal] public delegate void ItemSelectedEventHandler(int hash);

        public void AddModifiersToList(List<(string Mod, int Hash)> modifiers)
        {
            ClearList();
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
            foreach (var item in _container?.GetChildren().Cast<SelectableItem>() ?? [])
                item.SetSelectable(selectable);
        }

        public void UpdateSelectedItem((string Mod, int Hash) newModifier)
        {
            var selectableItem = _container?.GetChild<SelectableItem>(_lastSelectedChild);
            selectableItem?.SetText(newModifier.Mod);
            selectableItem?.SetMetadata(newModifier.Hash);
        }

        private void OnItemSelected(int index)
        {
            _lastSelectedChild = index;
            var hash = _container?.GetChild<SelectableItem>(index).GetMetadata().AsInt32() ?? 0;
            if (hash != 0)
            {
                EmitSignal(SignalName.ItemSelected, hash);
            }
        }

        public void ClearList()
        {
            _lastSelectedChild = -1;
            foreach (var child in _container?.GetChildren() ?? [])
                child.QueueFree();
        }
    }
}
