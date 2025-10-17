namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using System.Threading.Tasks;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;

    [Tool]
    [GlobalClass]
    public partial class CraftingItems : Control, IInitializable, IClosable, IRequireServices
    {
        private const string UID = "uid://dot5loe7a27rt";
        private TaskCompletionSource<IEnumerable<string>>? _selectedTcs;
        [Export] private ItemList? _items;
        [Export] private Button? _add, _cancel;
        private Dictionary<int, string> _resources = [];
        private IItemDataProvider? _itemDataProvider;
        private IInventory? _inventory;


        public event Action? Close;

        public override void _Ready()
        {
            if (_add != null) _add.Pressed += OnAddPressed;
            if (_cancel != null) _cancel.Pressed += OnCancelPressed;
        }


        public void InjectServices(Core.Interfaces.Data.IServiceProvider provider)
        {
            _itemDataProvider = provider.GetService<IItemDataProvider>();
            _inventory = provider.GetService<IInventory>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Setup(IEnumerable<string> disabledResources)
        {
            // TODO: I need not only Essences.
            var inventoryItems = _inventory?.GetAllItemIdsWithTag("Essence");
            foreach (var res in inventoryItems ?? [])
                AddItem(res);

            UpdateDisabled(disabledResources);
        }

        public Task<IEnumerable<string>> WaitForSelectionAsync()
        {
            _selectedTcs = new TaskCompletionSource<IEnumerable<string>>();

            return _selectedTcs.Task;
        }


        private void OnCancelPressed()
        {
            _selectedTcs?.TrySetResult([]);
            Close?.Invoke();
        }

        private void OnAddPressed()
        {
            _selectedTcs?.TrySetResult(GetSelectedIds());
            Close?.Invoke();
        }

        private void AddItem(string resourceId, bool selectable = true)
        {
            if (_items != null)
            {
                var displayName = Lokalizator.Lokalize(resourceId);
                var id = _items.AddItem(displayName, _itemDataProvider?.GetItemIcon(resourceId), selectable);
                _resources.Add(id, resourceId);
            }
        }

        private IEnumerable<string> GetSelectedIds()
        {
            List<string> result = [];
            var selected = _items?.GetSelectedItems() ?? [];
            for (int i = 0; i < selected.Length; i++)
            {
                result.Add(_resources[selected[i]]);
            }

            return result;
        }

        private void UpdateDisabled(IEnumerable<string> disabled)
        {
            foreach (var res in _resources)
            {
                if (disabled.Contains(res.Value))
                    _items?.SetItemDisabled(res.Key, true);
            }
        }
    }
}
