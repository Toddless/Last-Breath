namespace Crafting.Source.UIElements
{
    using Godot;
    using Crafting.Source.DI;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator.Events;

    public partial class InventoryWindow : Panel, IInitializable, IRequireServices
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private GridContainer? _inventoryGrid;

        private IInventory? _inventory;
        private IUiMediator? _uiMediator;

        public override void _Ready()
        {
            var dataProvider = ServiceProvider.Instance.GetService<IItemDataProvider>();

            _inventory?.Initialize(210, _inventoryGrid);
            if (_craftingButton != null)
                _craftingButton.Pressed += OnCraftingButtonPressed;

            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in dataProvider.GetAllResources())
                    _inventory?.TryAddItem(resource, 100);
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _uiMediator = ServiceProvider.Instance.GetService<IUiMediator>();
            _inventory = ServiceProvider.Instance.GetService<IInventory>();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnCraftingButtonPressed() => _uiMediator?.Publish(new OpenCraftingWindowEvent(string.Empty));
    }
}
