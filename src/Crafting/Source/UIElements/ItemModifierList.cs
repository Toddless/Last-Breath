namespace Crafting.Source.UIElements
{
    using System.Collections.Generic;
    using Godot;

    [GlobalClass]
    public partial class ItemModifierList : Control
    {
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
                item.Selected += OnItemSelected;
                _container?.AddChild(item);
            }
        }

        private void OnItemSelected(int index)
        {
            var hash = _container?.GetChild<SelectableItem>(index).GetMetadata().AsInt32() ?? 0;
            if (hash != 0) EmitSignal(SignalName.ItemSelected, hash);
        }

        public void ClearList()
        {
            foreach (var child in _container?.GetChildren() ?? [])
                child.QueueFree();
        }
    }
}
