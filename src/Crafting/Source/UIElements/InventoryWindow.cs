namespace Crafting.Source.UIElements
{
    using Godot;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator;

    public partial class InventoryWindow : Panel, IInitializable, IRequireServices
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private GridContainer? _inventoryGrid;

        private IMediator? _mediator;
        private IInventory? _inventory;
        private IItemDataProvider? _itemDataProvider;

        public override void _Ready()
        {
            _inventory?.Initialize(216, _inventoryGrid);
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
            _mediator = provider.GetService<IMediator>();
        }

        private void OnInventoryFull(InventoryFullEvent obj)
        {

        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnCraftingButtonPressed() => _mediator?.PublishAsync(new OpenCraftingWindowEvent(string.Empty));
    }
}
