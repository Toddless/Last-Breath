namespace Crafting.Source.UIElements.Layers
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Constants;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Crafting.TestResources.Inventory;
    using Godot;

    public partial class CraftingLayer : CanvasLayer
    {
        private readonly HashSet<IMaterialModifier> _mainModifiers = [];
        private readonly HashSet<IMaterialModifier> _optionalModifiers = [];

        private readonly List<ICraftingResource> _takenOptionalResources = [];
        private ICraftingRecipe? _currentSelectedRecipe;
        private CraftingResourceProvider? _resourceManager;
        private CraftingRecipeProvider? _recipeManager;
        private ItemProvider? _itemProvider;
        private Inventory? _craftingInventory;
        [Export] private CraftingUI? _craftingUI;

        public override void _Ready()
        {
            _resourceManager = new("res://TestResources/RecipeAndResources/Resources/");
            _recipeManager = new("res://TestResources/RecipeAndResources/Recipes/");
            _itemProvider = new("res://TestResources/RecipeAndResources/Items/");
            _recipeManager.InitializeRecipes();
            _resourceManager.InitializeResources();
            _itemProvider.Initialize();
            _craftingInventory = new Inventory();
            _craftingInventory.Initialize(30);
            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in _resourceManager.GetAllResources())
                {
                    var from = rnd.RandiRange(1, 15);
                    var to = rnd.RandiRange(15, 100);
                    var amount = rnd.RandiRange(from, to);
                    _craftingInventory.AddItem((IItem)resource, amount);
                }

            VisibilityChanged += OnVisibilityChanges;
            if (_craftingUI != null)
            {
                _craftingUI.RecipeSelected += OnRecipeSelected;
                _craftingUI.ItemCreated += OnItemCreatePressed;
            }

            for (int i = 0; i < 3; i++)
            {
                var opt = OptionalResource.Initialize().Instantiate<OptionalResource>();
                opt.ResourceRemoved += resource =>
                {
                    _takenOptionalResources.RemoveAll(res => res.Id == resource.Id);
                    OnResourceRemoved(resource);
                };
                opt.AddPressed += () => OnAddPressed(opt);
                _craftingUI?.AddOptionalResource(opt);
            }
            Hide();
        }

        // if we can press this button => we have enough resources
        private void OnItemCreatePressed()
        {
            ConsumeResourcesFromInventory();
            ClearUI();
            ShowCurrentRecipe();
        }

        private void ConsumeResourcesFromInventory()
        {
            foreach (var optResource in _takenOptionalResources)
            {
                _craftingInventory?.RemoveItem(optResource.Id);
            }
            _craftingUI?.ConsumeOptionalResource();

            foreach (var mainRes in _currentSelectedRecipe?.MainResource ?? [])
            {
                _craftingInventory?.RemoveItem(mainRes.CraftingResourceId, mainRes.Amount);
            }
        }

        private void ClearUI()
        {
            _craftingUI?.ClearOptionalResources();
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
            craftingItems.Setup(available, _takenOptionalResources, selected =>
            {
                opt.AddCraftingResource(selected, GetResourceAmountFromInventory(selected.Id));
                _takenOptionalResources.Add(selected);
                OnResourceAdded(selected);
                craftingItems.QueueFree();
            },
            craftingItems.QueueFree);
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
            _mainModifiers.Clear();
            var recipe = _recipeManager?.GetRecipe(id);
            _currentSelectedRecipe = recipe;
            ShowCurrentRecipe();
        }

        private void ShowCurrentRecipe()
        {
            var mainResources = PrepareRecipeToShow(_currentSelectedRecipe?.MainResource ?? []);
            _craftingUI?.ShowRecipe(mainResources);
            _craftingUI?.ShowModifiers(ConcatModifierSets());
            var allResourcesEnough = mainResources.Values.All(x => x.have >= x.need);
            if (mainResources.Count == 0) allResourcesEnough = false;
            _craftingUI?.SetCreateButtonState(allResourcesEnough);
        }

        private int GetResourceAmountFromInventory(string resourceId)
        {
            var item = _craftingInventory?.GetItemSlotById(resourceId);
            if (item == null) return 0;
            else return item.Quantity;
        }

        private HashSet<IMaterialModifier> ConcatModifierSets() => [.. _mainModifiers, .. _optionalModifiers];

        private IReadOnlyDictionary<ICraftingResource, (int have, int need)> PrepareRecipeToShow(List<IRecipeRequirement> recipeRequirements)
        {
            var resourcesInInventory = new Dictionary<ICraftingResource, (int having, int needed)>();
            foreach (var requirement in recipeRequirements)
            {
                var requiredResourceId = requirement.CraftingResourceId;
                var resource = _resourceManager?.GetResource(requiredResourceId);
                if (resource == null) continue;
                resourcesInInventory.Add(resource, (GetResourceAmountFromInventory(requiredResourceId), requirement.Amount));
                AddModifiersToSet(_mainModifiers, resource.MaterialType?.Modifiers ?? []);
            }
            return resourcesInInventory;
        }

        private void OnResourceAdded(ICraftingResource resource)
        {
            AddModifiersToSet(_optionalModifiers, resource.MaterialType?.Modifiers ?? []);
            _craftingUI?.ShowModifiers(ConcatModifierSets());
        }

        private void OnResourceRemoved(ICraftingResource resource)
        {
            RemoveModifiersFromSet(_optionalModifiers, resource.MaterialType?.Modifiers ?? []);
            _craftingUI?.ShowModifiers(ConcatModifierSets());
        }

        private void AddModifiersToSet(HashSet<IMaterialModifier> set, IReadOnlyList<IMaterialModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                if (set.Contains(modifier)) continue;
                set.Add(modifier);
            }
        }

        private void RemoveModifiersFromSet(HashSet<IMaterialModifier> set, IReadOnlyList<IMaterialModifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                set.Remove(modifier);
            }
        }
    }
}
