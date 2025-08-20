namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        public override void _ExitTree()
        {
            if (_add != null) _add.Pressed -= OnAddPressed;
            if (_cancel != null) _cancel.Pressed -= OnCancelPressed;
            _resources.Clear();
            _onCancel = null;
            _onSelect = null;
        }

        private void OnCancelPressed() => _onCancel?.Invoke();

        private void OnAddPressed()
        {
            if (_items == null) return;
            var selected = _items.GetSelectedItems();
            if (selected.Length == 0) return;
            var idx = selected[0];
            if (idx < 0 || idx >= _resources.Count) return;

            _onSelect?.Invoke(_resources[idx]);
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
            {
                if (disabled.Contains(_resources[i]))
                {
                    _items?.SetItemDisabled(i, true);
                }
            }
        }
    }
}
