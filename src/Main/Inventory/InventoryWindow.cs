namespace LastBreath.Inventory
{
    using Core.Data;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.UI;
    using Godot;

    public partial class InventoryWindow : Panel, IWindow
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private GridContainer? _inventoryGrid;

        private IGameMessageBus? _messageBus;
        private IInventory? _inventory;
        private IItemDataProvider? _itemDataProvider;

        public bool IsAlreadyVisible => IsInsideTree() && Visible;

        public override void _Ready()
        {
            if (_craftingButton != null)
                _craftingButton.Pressed += OnCraftingButtonPressed;

            using var rnd = new RandomNumberGenerator();
            foreach (var resource in _itemDataProvider?.GetAllResources() ?? [])
                _inventory?.TryAddItem(resource, 100);
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _inventory = provider.GetService<IInventory>();
            _itemDataProvider = provider.GetService<IItemDataProvider>();
            _messageBus = provider.GetService<IGameMessageBus>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
        public void Close() => GetParent().RemoveChild(this);
        private void OnCraftingButtonPressed() => _messageBus?.PublishMessageAsync(new OpenCraftingWindowMessage(string.Empty));
    }
}
