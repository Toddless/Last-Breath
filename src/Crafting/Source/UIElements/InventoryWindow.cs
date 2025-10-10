namespace Crafting.Source.UIElements
{
    using Godot;
    using Core.Interfaces.UI;
    using Crafting.TestResources.Inventory;
    using Crafting.TestResources.DI;

    public partial class InventoryWindow : Panel, IInitializable
    {
        private const string UID = "uid://byx7g1b2wlwfl";

        [Export] private Button? _craftingButton, _allStatsButton, _sortButton, _destroyButton;
        [Export] private GridContainer? _inventoryGrid;
        private ItemInventory? _inventory;
        private ItemDataProvider? _dataProvider;
        private UIElementProvider? _elementProvider;

        public override void _Ready()
        {
            _inventory = ServiceProvider.Instance.GetService<ItemInventory>();
            _dataProvider = ServiceProvider.Instance.GetService<ItemDataProvider>();
            _elementProvider = ServiceProvider.Instance.GetService<UIElementProvider>();
            _inventory.Initialize(210, _inventoryGrid);

            if (_craftingButton != null)
                _craftingButton.Pressed += OnCraftingButtonPressed;

            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in ItemDataProvider.Instance?.GetAllResources() ?? [])
                {
                    _inventory.TryAddItem(resource, 100);
                }
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void OnCraftingButtonPressed()
        {
            var recipeTree = _elementProvider?.CreateSingleClosableOrGet<RecipiesWindow>(GetParent());
            recipeTree?.CreateRecipeTree(_dataProvider?.GetCraftingRecipes() ?? []);
        }
    }
}
