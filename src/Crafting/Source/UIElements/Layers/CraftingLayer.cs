namespace Crafting.Source.UIElements.Layers
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
    using Core.Constants;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Crafting.TestResources.Inventory;
    using Core.Interfaces;

    public partial class CraftingLayer : CanvasLayer
    {
        private readonly HashSet<IMaterialModifier> _mainModifiers = [];
        private readonly HashSet<IMaterialModifier> _optionalModifiers = [];

        private readonly List<ICraftingResource> _takenOptionalResources = [];
        private readonly List<ICraftingResource> _mainResources = [];
        private ICraftingRecipe? _currentSelectedRecipe;

        private EquipItemDataProvider? _dataProvider;
        private CraftingResourceProvider? _resourceManager;
        private CraftingRecipeProvider? _recipeManager;
        private ItemCreator? _itemCreator;
        private IInventory? _craftingInventory;
        private ICharacter? _player;
        [Export] private CraftingUI? _craftingUI;

        public override void _Ready()
        {
            // TODO: I need to change UI state if we see any popup
            _resourceManager = new("res://TestResources/RecipeAndResources/Resources/");
            _recipeManager = new("res://TestResources/RecipeAndResources/Recipes/");
            _dataProvider = new("res://TestResources/RecipeAndResources/Items/");
            var inventory = new Inventory();
            inventory.Initialize(30);
            _craftingInventory = inventory;
            _itemCreator = new ItemCreator();
            _dataProvider.LoadData();
            _recipeManager.InitializeRecipes();
            _resourceManager.InitializeResources();
            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in _resourceManager.GetAllResources())
                    _craftingInventory.AddItem((IItem)resource, 100);

            // it is work only if we have separate inventories (for resources, equip, etc)
            VisibilityChanged += OnVisibilityChanges;
            if (_craftingUI != null)
            {
                _craftingUI.RecipeSelected += OnRecipeSelected;
                _craftingUI.ItemCreated += OnItemCreatePressed;
                _craftingUI.ChangeLanguage += OnLaguageChanged;
            }

            for (int i = 0; i < 3; i++)
            {
                var opt = OptionalResource.Initialize().Instantiate<OptionalResource>();
                opt.ResourceRemoved += OnResourceRemoved;
                opt.AddPressed += () => OnAddPressed(opt);
                _craftingUI?.AddOptionalResource(opt);
            }
            Hide();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_crafting"))
            {
                if (Visible) Hide();
                else Show();
            }
        }

        // if we can press this button => we have enough resources
        private void OnItemCreatePressed()
        {
            // we should have all data at this point, but just in case
            if (_currentSelectedRecipe == null)
            {
                Logger.LogNull(nameof(_currentSelectedRecipe), this);
                return;
            }

            // What if i need to create item instead of EquipItem??
            if (!CreateItem(_currentSelectedRecipe))
            {
                // TODO: Raise event "Item creation failed"
                return;
            }

            ConsumeResourcesFromInventory();
            ShowCurrentRecipe();
        }

        private bool CreateItem(ICraftingRecipe recipe)
        {
            IItem? item = null;
            switch (true)
            {
                // TODO: Creation for generic items not working, i have no resources
                case var _ when recipe.Tags.Contains(TagConstants.Equipment, StringComparer.OrdinalIgnoreCase):
                    item = recipe.Tags.Contains("Generic") ?
                        _itemCreator?.CreateGenericItem(recipe, _mainResources, _takenOptionalResources) :
                        _itemCreator?.CreateEquipItem(recipe, _mainResources, _takenOptionalResources);
                    break;
                case var _ when recipe.Tags.Contains("Item"):
                    item = _itemCreator?.CreateItem(recipe);
                    break;
            }

            if (item == null)
            {
                Logger.LogError($"Failed to create item: {recipe.ResultItemId}. Recipe: {recipe.Id}", this);
                return false;
            }

            var notifier = ItemCreatedNotifier.Initialize().Instantiate<ItemCreatedNotifier>();
            notifier.SetImage(item.FullImage ?? item.Icon);
            // TODO: Should not be here
            if (item is IEquipItem equipItem)
            {
                var baseStats = new Label
                {
                    Text = Lokalizator.Lokalize("BaseStats")
                };
                notifier.SetText(baseStats);

                foreach (var baseStat in equipItem.BaseModifiers)
                {
                    var label = new Label
                    {
                        Text = Lokalizator.Format(baseStat)
                    };
                    notifier.SetText(label);
                }

                var additional = new Label
                {
                    Text = Lokalizator.Lokalize("RandomStats")
                };
                notifier.SetText(additional);

                foreach (var addStats in equipItem.AdditionalModifiers)
                {
                    var label = new Label
                    {
                        Text = Lokalizator.Format(addStats)
                    };
                    notifier.SetText(label);
                }
            }

            _player?.AddItemToInventory(item);

            CallDeferred(MethodName.AddChild, notifier);
            return true;
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

        private void ShowCurrentRecipe()
        {
            // TODO: remember about generic items. they have special icon,
            // usual items have no optional resources, have no modifiers
            var itemId = _currentSelectedRecipe?.ResultItemId ?? string.Empty;
            var templates = CreateResourceTemplates(_currentSelectedRecipe?.MainResource ?? []);
            var icon = _dataProvider?.GetItemImage(itemId);
            _craftingUI?.ShowRecipe(templates.Keys);
            _craftingUI?.ShowModifiers(FormattedModifiers());
            ShowItemStats();
            if (icon != null) _craftingUI?.SetItemIcon(icon);
            _craftingUI?.SetItemDescription(Lokalizator.LokalizeDescription(itemId));
            var allResourcesEnough = templates.Values.All(x => x.have >= x.need);
            if (templates.Count == 0) allResourcesEnough = false;
            _craftingUI?.SetCreateButtonState(allResourcesEnough);
        }

        private Dictionary<ResourceTemplateUI, (int have, int need)> CreateResourceTemplates(List<IRecipeRequirement> recipeRequirements)
        {
            var templates = new Dictionary<ResourceTemplateUI, (int have, int need)>();
            foreach (var requirement in recipeRequirements)
            {
                var requiredResourceId = requirement.CraftingResourceId;
                var resource = _mainResources.FirstOrDefault(x => x.Id == requiredResourceId);
                if (resource == null)
                {
                    resource = _resourceManager?.GetResource(requiredResourceId);
                    if (resource == null) continue;
                    _mainResources.Add(resource);
                }

                var template = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
                var amountHave = GetResourceAmountFromInventory(requiredResourceId);
                template.SetText(resource.DisplayName, amountHave, requirement.Amount);
                if (resource.Icon != null) template.SetIcon(resource.Icon);
                templates.Add(template, (amountHave, requirement.Amount));

                AddModifiersToSet(_mainModifiers, resource.MaterialType?.Modifiers ?? []);
            }
            return templates;
        }

        private void ShowItemStats()
        {
            var stats = _dataProvider?.GetItemBaseStats(_currentSelectedRecipe?.ResultItemId ?? string.Empty);

            foreach (var item in stats ?? [])
            {
                var label = new Label
                {
                    Text = item,
                    SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                };
                _craftingUI?.AddBaseStatLabel(label);
            }
        }

        private int GetResourceAmountFromInventory(string resourceId)
        {
            var item = _craftingInventory?.GetSlotWithItemOrNull(resourceId);
            if (item == null) return 0;
            return item.Quantity;
        }

        private HashSet<IMaterialModifier> ConcatModifierSets() => [.. _mainModifiers, .. _optionalModifiers];

        private void OnResourceAdded(ICraftingResource resource)
        {
            AddModifiersToSet(_optionalModifiers, resource.MaterialType?.Modifiers ?? []);
            _craftingUI?.ShowModifiers(FormattedModifiers());
        }

        private void OnResourceRemoved(ICraftingResource resource)
        {
            RemoveModifiersFromSet(_optionalModifiers, resource.MaterialType?.Modifiers ?? []);
            _takenOptionalResources.RemoveAll(res => res.Id == resource.Id);
            _craftingUI?.ShowModifiers(FormattedModifiers());
        }

        private void OnAddPressed(OptionalResource opt)
        {
            var itemSlots = _craftingInventory?.GetAllSlotsWithItemsWithTag(TagConstants.Essence);

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

        private void OnLaguageChanged()
        {
            var currentLang = TranslationServer.GetLocale();
            if (currentLang == "en_GB") TranslationServer.SetLocale("ru");
            else TranslationServer.SetLocale("en_GB");
        }

        private void OnRecipeSelected(string id)
        {
            _mainModifiers.Clear();
            _mainResources.Clear();
            _craftingUI?.ClearItemIcon();
            var recipe = _recipeManager?.GetRecipe(id);
            _currentSelectedRecipe = recipe;
            ShowCurrentRecipe();
        }

        private void OnVisibilityChanges()
        {
            if (Visible)
                _craftingUI?.CreatingRecipeTree(_recipeManager?.Recipes);
            else
            {
                _currentSelectedRecipe = null;
                _craftingUI?.ClearUI();
            }
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

        private IEnumerable<string> FormattedModifiers()
        {
            var formattedStrings = new List<string>();
            foreach (var mod in ConcatModifierSets())
            {
                var formatted = Lokalizator.Format(mod);
                formattedStrings.Add(formatted);
            }
            return formattedStrings;
        }
    }
}
