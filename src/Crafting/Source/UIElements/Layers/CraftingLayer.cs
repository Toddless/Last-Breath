namespace Crafting.Source.UIElements.Layers
{
    using System.Collections.Generic;
    using Core.Constants;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Crafting.TestResources.Inventory;
    using Godot;

    public partial class CraftingLayer : CanvasLayer
    {
        private readonly List<ICraftingResource> _takenResources = [];
        private ResourceManager? _resourceManager;
        private RecipeManager? _recipeManager;
        private Inventory? _craftingInventory;
        [Export] private CraftingUI? _craftingUI;

        public override void _Ready()
        {
            _resourceManager = new("res://TestResources/RecipeAndResources/Resources/");
            _recipeManager = new("res://TestResources/RecipeAndResources/Recipes/");
            _recipeManager.InitializeRecipes();
            _resourceManager.InitializeResources();
            _craftingInventory = new Inventory();
            _craftingInventory.Initialize(30);
            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in _resourceManager.GetAllResources())
                {
                    _craftingInventory.AddItem((IItem)resource, rnd.RandiRange(2, 100));
                }

            var res = _resourceManager?.GetResource("Crafting_Resource_Deer_Leather") as IItem;
            if (res != null)
            {
                _craftingInventory.AddItem(res, 1000);
            }
            VisibilityChanged += OnVisibilityChanges;
            if (_craftingUI != null) _craftingUI.RecipeSelected += OnRecipeSelected;

            for (int i = 0; i < 3; i++)
            {
                var opt = OptionalResource.Initialize().Instantiate<OptionalResource>();
                opt.ResourceAdded += resource =>
                {
                    _takenResources.Add(resource);
                    _craftingUI?.OnResourceAdded(resource);
                };
                opt.ResourceRemoved += resource =>
                {
                    _takenResources.RemoveAll(res => res.Id == resource.Id);
                    _craftingUI?.OnResourceRemoved(resource);
                };
                opt.AddPressed += () => OnAddPressed(opt);
                _craftingUI?.AddOptionalResource(opt);
            }

        }

        private void OnAddPressed(OptionalResource opt)
        {
            var itemSlots = _craftingInventory?.GetAllItemSlotsWithTag(TagConstants.Essence);

            var available = new List<ICraftingResource>();
            foreach (var slot in itemSlots ?? [])
            {
                if (slot.CurrentItem is ICraftingResource resource)
                    available.Add(resource);
            }

            var craftingItems = CraftingItems.Initialize().Instantiate<CraftingItems>();
            craftingItems.Setup(available, _takenResources, selected =>
            {
                opt.AddCraftingResource(selected);
                _takenResources.Add(selected);
                craftingItems.QueueFree();
            }, () =>
            {
                craftingItems?.QueueFree();
            });

            AddChild(craftingItems);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_crafting"))
            {
                if (Visible) Hide();
                else Show();
            }
        }

        private void OnVisibilityChanges()
        {
            if (Visible)
                _craftingUI?.CreatingRecipeTree(_recipeManager?.Recipes);
            else
                _craftingUI?.DestroingRecipeTree();
        }

        private void OnRecipeSelected(string id)
        {
            var recipe = _recipeManager?.GetRecipe(id);
            var mainRes = GetRecipeRequiredResources(recipe?.MainResource ?? []);
            if (recipe != null) _craftingUI?.ShowRecipe(recipe, mainRes);
        }


        private IReadOnlyDictionary<ICraftingResource, int> GetRecipeRequiredResources(List<IRecipeRequirement> list)
        {
            var resources = new Dictionary<ICraftingResource, int>();
            foreach (var item in list)
            {
                var res = _resourceManager?.GetResource(item.CraftingResourceId);
                if (res == null) continue;
                // TODO: Instead amount i need to get info from player inventory
                var inInventory = _craftingInventory?.GetItemSlotById(item.CraftingResourceId);
                if (inInventory == null)
                    resources.Add(res, 0);
                else
                    resources.Add(res, inInventory.Quantity);
            }

            return resources;
        }
    }
}
