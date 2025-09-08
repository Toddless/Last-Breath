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

    public partial class CraftingLayer : CanvasLayer
    {
        private readonly HashSet<IMaterialModifier> _mainModifiers = [];
        private readonly HashSet<IMaterialModifier> _optionalModifiers = [];

        private readonly HashSet<string> _takenOptionalResources = [];
        private readonly HashSet<string> _mainResources = [];
        private string? _currentSelectedRecipe;

        private ItemDataProvider? _dataProvider;
        private ItemCreator? _itemCreator;
        private IInventory? _itemInventory;
        [Export] private CraftingUI? _craftingUI;

        public override void _Ready()
        {
            _dataProvider = new("res://TestResources/RecipeAndResources/");
            _itemCreator = new ItemCreator();
            _dataProvider.LoadData();

            VisibilityChanged += OnVisibilityChanges;
            if (_craftingUI != null)
            {
                _craftingUI.RecipeSelected += OnRecipeSelected;
                _craftingUI.ItemCreated += OnItemCreatePressed;
            }

            for (int i = 0; i < 3; i++)
            {
                var opt = OptionalResource.Initialize().Instantiate<OptionalResource>();
                opt.ResourceRemoved += OnResourceRemoved;
                opt.AddPressed += () => OnAddPressed(opt);
                _craftingUI?.AddOptionalResource(opt);
            }

            // for now
            // ____________________________________________
            var itemInventory = new ItemInventory();
            _itemInventory = itemInventory;
            _craftingUI?.InitializeInventory(itemInventory);

            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in _dataProvider.GetAllResources())
                    _itemInventory.AddItem(resource, 100);
            // ____________________________________________
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
            if (string.IsNullOrWhiteSpace(_currentSelectedRecipe))
            {
                Logger.LogNull(nameof(_currentSelectedRecipe), this);
                return;
            }

            // What if i need to create item instead of EquipItem??
            var item = CreateItem(_currentSelectedRecipe);
            if (item == null)
            {
                Logger.LogError($"Failed to create an item. Recipe: {_currentSelectedRecipe}", this);
                return;
            }
            CreateNotifier(item);
            ConsumeResourcesFromInventory();
            ShowCurrentRecipe();
        }

        private void CreateNotifier(IItem item)
        {
            var notifier = ItemCreatedNotifier.Initialize().Instantiate<ItemCreatedNotifier>();
            notifier.SetImage(item.FullImage ?? item.Icon);
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
            notifier.OkButtonPressed += Add;
            notifier.DestroyButtonPressed += Destroy;

            void Add()
            {
                _itemInventory?.AddItem(item);
                Unsubscribe();
            }

            void Destroy()
            {
                DestroyItem(item);
                Unsubscribe();
            }

            void Unsubscribe()
            {
                notifier.OkButtonPressed -= Add;
                notifier.DestroyButtonPressed -= Destroy;
            }

            CallDeferred(MethodName.AddChild, notifier);
        }

        private void DestroyItem(IItem item)
        {

        }

        private IItem? CreateItem(string recipeId)
        {
            IItem? item = null;
            var recipe = (ICraftingRecipe?)_dataProvider?.CopyBaseItem(_currentSelectedRecipe ?? string.Empty);
            if (recipe == null) return null;
            switch (true)
            {
                // TODO: Creation for generic items not working, i have no resources
                case var _ when recipe.Tags.Contains(TagConstants.Equipment, StringComparer.OrdinalIgnoreCase):
                    item = recipe.Tags.Contains("Generic") ?
                        _itemCreator?.CreateGenericItem(recipe, GetCraftingResources(_mainResources), GetCraftingResources(_takenOptionalResources)) :
                        _itemCreator?.CreateEquipItem(recipe, GetCraftingResources(_mainResources), GetCraftingResources(_takenOptionalResources));
                    break;
                case var _ when recipe.Tags.Contains("Item"):
                    item = _itemCreator?.CreateItem(recipe);
                    break;
            }
            return item;
        }

        private IEnumerable<ICraftingResource> GetCraftingResources(HashSet<string> resourceIds)
        {
            var resources = new List<ICraftingResource>();
            foreach (var res in resourceIds)
            {
                var loadded = _dataProvider?.CopyBaseItem(res);
                if (loadded != null) resources.Add((ICraftingResource)loadded);
            }

            return resources;
        }

        private void ConsumeResourcesFromInventory()
        {
            foreach (var res in _dataProvider?.GetRecipeRequirements(_currentSelectedRecipe ?? string.Empty) ?? [])
            {
                _itemInventory?.RemoveItem(res.CraftingResourceId, res.Amount);
            }
            foreach (var optResourceId in _takenOptionalResources)
            {
                _itemInventory?.RemoveItem(optResourceId);
            }
            _craftingUI?.ConsumeOptionalResource();

        }

        private void ShowCurrentRecipe()
        {
            // TODO: remember about generic items. they have special icon,
            // usual items have no optional resources, have no modifiers
            var itemId = _dataProvider?.GetRecipeResultItemId(_currentSelectedRecipe ?? string.Empty) ?? string.Empty;
            var templates = CreateResourceTemplates(_dataProvider?.GetRecipeRequirements(_currentSelectedRecipe ?? string.Empty) ?? []);
            var icon = _dataProvider?.GetItemIcon(itemId);


            _craftingUI?.ShowRecipe(templates.Keys);
            _craftingUI?.ShowModifiers(FormattedModifiers());
            ShowItemStats();
            //if (icon != null) _craftingUI?.SetItemIcon(icon);
            _craftingUI?.SetItemDescription(Lokalizator.LokalizeDescription(itemId));
            var allResourcesEnough = templates.Values.All(x => x.have >= x.need);
            if (templates.Count == 0) allResourcesEnough = false;
            _craftingUI?.SetCreateButtonState(allResourcesEnough);
        }

        private Dictionary<ResourceTemplateUI, (int have, int need)> CreateResourceTemplates(List<IRecipeRequirement> recipeRequirements)
        {
            var templates = new Dictionary<ResourceTemplateUI, (int have, int need)>();
            foreach (var req in recipeRequirements)
            {
                var reqResourceId = req.CraftingResourceId;
                _mainResources.Add(reqResourceId);

                var resourceDisplayName = _dataProvider?.GetItemDisplayName(reqResourceId) ?? string.Empty;
                var resourceIcon = _dataProvider?.GetItemIcon(reqResourceId);
                var template = ResourceTemplateUI.Initialize().Instantiate<ResourceTemplateUI>();
                var amountHave = _itemInventory?.GetTotalItemAmount(reqResourceId) ?? 0;
                var resourceModifiers = _dataProvider?.GetResourceModifiers(reqResourceId) ?? [];

                template.SetText(resourceDisplayName, amountHave, req.Amount);
                if (resourceIcon != null) template.SetIcon(resourceIcon);
                templates.Add(template, (amountHave, req.Amount));

                AddModifiersToSet(_mainModifiers, resourceModifiers);
            }
            return templates;
        }

        private void ShowItemStats()
        {
            var resultItemId = _dataProvider?.GetRecipeResultItemId(_currentSelectedRecipe ?? string.Empty) ?? string.Empty;
            var stats = _dataProvider?.GetItemBaseStats(resultItemId);

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
            var item = _itemInventory?.GetSlotWithItemOrNull(resourceId);
            if (item == null) return 0;
            return item.Quantity;
        }

        private HashSet<IMaterialModifier> ConcatModifierSets() => [.. _mainModifiers, .. _optionalModifiers];

        private void OnResourceAdded(string resource)
        {
            var modifiers = _dataProvider?.GetResourceModifiers(resource) ?? [];
            AddModifiersToSet(_optionalModifiers, modifiers);
            _craftingUI?.ShowModifiers(FormattedModifiers());
        }

        private void OnResourceRemoved(string resource)
        {
            var modifiers = _dataProvider?.GetResourceModifiers(resource) ?? [];
            RemoveModifiersFromSet(_optionalModifiers, modifiers);
            _takenOptionalResources.RemoveWhere(res => res == resource);
            _craftingUI?.ShowModifiers(FormattedModifiers());
        }

        private void OnAddPressed(OptionalResource opt)
        {
            var available = _itemInventory?.GetAllItemIdsWithTag(TagConstants.Essence) ?? [];

            var craftingItems = CraftingItems.Initialize().Instantiate<CraftingItems>();
            craftingItems.Setup(available, _takenOptionalResources, selected =>
            {
                opt.AddCraftingResource(selected, id => _itemInventory?.GetTotalItemAmount(id) ?? 0);
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
           // _craftingUI?.RemoveItemIcon();
            _currentSelectedRecipe = id;
            ShowCurrentRecipe();
        }

        private void OnVisibilityChanges()
        {
            if (Visible)
                _craftingUI?.CreatingRecipeTree(_dataProvider?.GetCraftingRecipes());
            else
            {
                _currentSelectedRecipe = null;
                _craftingUI?.ClearUI();
            }
        }

        private void AddModifiersToSet(HashSet<IMaterialModifier> set, IReadOnlyList<IMaterialModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                set.Add(modifier);
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
