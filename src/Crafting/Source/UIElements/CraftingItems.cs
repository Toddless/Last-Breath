namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
    using Core.Interfaces.UI;
    using System.Threading.Tasks;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Data;

    [GlobalClass]
    public partial class CraftingItems : Control, IInitializable, IClosable, IRequireServices
    {
        private const string UID = "uid://dot5loe7a27rt";
        private TaskCompletionSource<IEnumerable<string>>? _selectedItems;
        [Export] private ItemList? _items;
        [Export] private Button? _add, _cancel;
        private readonly Dictionary<int, string> _resources = [];
        private IItemDataProvider? _itemDataProvider;
        private IInventory? _inventory;


        public event Action? Close;

        public override void _Ready()
        {
            _add?.Pressed += OnAddPressed;
            _cancel?.Pressed += OnCancelPressed;
        }


        public void InjectServices(IGameServiceProvider provider)
        {
            _itemDataProvider = provider.GetService<IItemDataProvider>();
            _inventory = provider.GetService<IInventory>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public void Setup(IEnumerable<string> disabledResources)
        {
            // TODO: I need not only Essences.
            var inventoryItems = _inventory?.GetAllItemIdsWithTag("Essence");
            foreach (string res in inventoryItems ?? [])
                AddItem(res);

            UpdateDisabled(disabledResources);
        }

        public Task<IEnumerable<string>> WaitForSelectionAsync()
        {
            _selectedItems = new TaskCompletionSource<IEnumerable<string>>();

            return _selectedItems.Task;
        }


        private void OnCancelPressed()
        {
            _selectedItems?.TrySetResult([]);
            Close?.Invoke();
        }

        private void OnAddPressed()
        {
            _selectedItems?.TrySetResult(GetSelectedIds());
            Close?.Invoke();
        }

        private void AddItem(string resourceId, bool selectable = true)
        {
            if (_items == null) return;

            string displayName = Localization.Localize(resourceId);
            int id = _items.AddItem(displayName, _itemDataProvider?.GetItemIcon(resourceId), selectable);
            _resources.Add(id, resourceId);
        }

        private IEnumerable<string> GetSelectedIds()
        {
            List<string> result = [];
            int[] selected = _items?.GetSelectedItems() ?? [];
            result.AddRange(selected.Select(t => _resources[t]));
            return result;
        }

        private void UpdateDisabled(IEnumerable<string> disabled)
        {
            foreach (var res in _resources.Where(res => disabled.Contains(res.Value)))
                _items?.SetItemDisabled(res.Key, true);
        }
    }
}
