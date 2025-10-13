namespace Crafting.Source.UIElements
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.UI;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Crafting.TestResources.DI;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator.Requests;
    using Core.Interfaces.Data;

    public partial class InventoryWindow : Panel, IInitializable
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private GridContainer? _inventoryGrid;

        private IInventory? _inventory;
        private IUiMediator? _uiMediator;

        public override void _Ready()
        {
            _inventory = ServiceProvider.Instance.GetService<IInventory>();
            var dataProvider = ServiceProvider.Instance.GetService<IItemDataProvider>();
            _uiMediator = ServiceProvider.Instance.GetService<IUiMediator>();

            _inventory.Initialize(210, _inventoryGrid);
            _inventory.ItemInteraction += OnInventoryItemInteraction;
            if (_craftingButton != null)
                _craftingButton.Pressed += OnCraftingButtonPressed;

            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in dataProvider.GetAllResources())
                    _inventory.TryAddItem(resource, 100);
        }

        private void OnInventoryItemInteraction(IItem item, MouseInteractions interactions)
        {
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnCraftingButtonPressed() => _uiMediator?.Send(new OpenWindowRequest(typeof(RecipiesWindow)));
    }
}
