namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using Core.Interfaces.Crafting;
    using Godot;

    [Tool]
    [GlobalClass]
    public partial class CraftingItems : Control
    {
        private const string UID = "uid://dot5loe7a27rt";
        [Export] private ItemList? _items;
        [Export] private Button? _add, _cancel;
        private List<ICraftingResource> _resources = [];
        private Action<ICraftingResource>? _onSelect;
        private Action? _onCancel;

        public override void _Ready()
        {
            _add.Pressed += OnAddPressed;
            _cancel.Pressed += OnCancelPressed;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void AddItem(ICraftingResource resource, bool selectable = true)
        {
            _resources.Add(resource);
            _items?.AddItem(resource.DisplayName, resource.Icon, selectable);
        }

        private void OnCancelPressed() => _onCancel?.Invoke();

        private void OnAddPressed()
        {
            if (_items == null) return;
            var selected = _items.GetSelectedItems();
            if (selected.Length == 0) return;

            var idx = selected[0];

            if (idx < 0 || idx >= _resources.Count) return;

            var resource = _resources[idx];

            if (_items.IsItemDisabled(idx)) return;

            _onSelect?.Invoke(resource);
        }

        public void Setup(IEnumerable<ICraftingResource> resources,
            IEnumerable<ICraftingResource> disabledResources,
            Action<ICraftingResource> onSelect,
            Action onCancel)
        {
            _onSelect = onSelect;
            _onCancel = onCancel;

            foreach (var res in resources)
                AddItem(res);

            UpdateDisabled(disabledResources);
        }


        private void UpdateDisabled(IEnumerable<ICraftingResource> disabled)
        {
            for (int i = 0; i < _resources.Count; i++)
                _items?.SetItemDisabled(i, false);


            foreach (var dis in disabled)
            {
                for (int i = 0; i < _resources.Count; i++)
                {
                    if (_resources[i].Id == dis.Id)
                    {
                        _items?.SetItemDisabled(i, true);
                        break;
                    }
                }
            }
        }
    }
}
