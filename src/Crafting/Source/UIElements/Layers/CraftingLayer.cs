namespace Crafting.Source.UIElements.Layers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Constants;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Crafting.TestResources.Inventory;
    using Godot;
    using Utilities;

    public partial class CraftingLayer : CanvasLayer
    {
        private readonly HashSet<IMaterialModifier> _mainModifiers = [];
        private readonly HashSet<IMaterialModifier> _optionalModifiers = [];

        private readonly List<string> _takenOptionalResources = [];
        private readonly List<ICraftingResource> _mainResources = [];
        private ICraftingRecipe? _currentSelectedRecipe;

        private ItemDataProvider? _dataProvider;
        private ItemCreator? _itemCreator;
        //  private IInventory? _craftingInventory;
        private IInventory? _itemInventory;
        private ICharacter? _player;
        [Export] private CraftingUI? _craftingUI;

        public override void _Ready()
        {
            _dataProvider = new("res://TestResources/RecipeAndResources/");
            //var inventory = new Inventory();
            //inventory.Initialize(30);
            //_craftingInventory = inventory;
            _itemCreator = new ItemCreator();
            _dataProvider.LoadData();

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
            if (_currentSelectedRecipe == null)
            {
                Logger.LogNull(nameof(_currentSelectedRecipe), this);
                return;
            }

            // What if i need to create item instead of EquipItem??
            var item = CreateItem(_currentSelectedRecipe);
            if (item == null)
            {
                Logger.LogError($"Failed to create an item. Recipe: {_currentSelectedRecipe.Id}", this);
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

        private IItem? CreateItem(ICraftingRecipe recipe)
        {
            IItem? item = null;
            switch (true)
            {
                // TODO: Creation for generic items not working, i have no resources
                case var _ when recipe.Tags.Contains(TagConstants.Equipment, StringComparer.OrdinalIgnoreCase):
                    item = recipe.Tags.Contains("Generic") ?
                        _itemCreator?.CreateGenericItem(recipe, _mainResources, GetCraftingResources()) :
                        _itemCreator?.CreateEquipItem(recipe, _mainResources, GetCraftingResources());
                    break;
                case var _ when recipe.Tags.Contains("Item"):
                    item = _itemCreator?.CreateItem(recipe);
                    break;
            }
            return item;
        }

        private IEnumerable<ICraftingResource> GetCraftingResources()
        {
            var resources = new List<ICraftingResource>();
            foreach (var res in _takenOptionalResources)
            {
                var loadded = _dataProvider?.GetItem(res);
                if (loadded != null) resources.Add((ICraftingResource)loadded);
            }

            return resources;
        }

        private void ConsumeResourcesFromInventory()
        {
            foreach (var optResource in _takenOptionalResources)
            {
                _itemInventory?.RemoveItem(optResource);
            }
            _craftingUI?.ConsumeOptionalResource();

            foreach (var mainRes in _currentSelectedRecipe?.MainResource ?? [])
            {
                _itemInventory?.RemoveItem(mainRes.CraftingResourceId, mainRes.Amount);
            }
        }

        private void ShowCurrentRecipe()
        {
            // TODO: remember about generic items. they have special icon,
            // usual items have no optional resources, have no modifiers
            var itemId = _currentSelectedRecipe?.ResultItemId ?? string.Empty;
            var templates = CreateResourceTemplates(_currentSelectedRecipe?.MainResource ?? []);
            var icon = _dataProvider?.GetItemIcon(itemId);
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
                    resource = (ICraftingResource?)_dataProvider?.GetItem(requiredResourceId);
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
            var item = _itemInventory?.GetSlotWithItemOrNull(resourceId);
            if (item == null) return 0;
            return item.Quantity;
        }

        private HashSet<IMaterialModifier> ConcatModifierSets() => [.. _mainModifiers, .. _optionalModifiers];

        private void OnResourceAdded(string resource)
        {
            var modifiers = CraftingResourceProvider.Instance?.GetResourceModifiers(resource) ?? [];
            AddModifiersToSet(_optionalModifiers, modifiers);
            _craftingUI?.ShowModifiers(FormattedModifiers());
        }

        private void OnResourceRemoved(string resource)
        {
            var modifiers = CraftingResourceProvider.Instance?.GetResourceModifiers(resource) ?? [];
            RemoveModifiersFromSet(_optionalModifiers, modifiers);
            _takenOptionalResources.RemoveAll(res => res == resource);
            _craftingUI?.ShowModifiers(FormattedModifiers());
        }

        private void OnAddPressed(OptionalResource opt)
        {
            var available = _itemInventory?.GetAllItemIdsWithTag(TagConstants.Essence) ?? [];

            var craftingItems = CraftingItems.Initialize().Instantiate<CraftingItems>();
            craftingItems.Setup(available, _takenOptionalResources, selected =>
            {
                opt.AddCraftingResource(selected, GetResourceAmountFromInventory(selected));
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
            var recipe = (ICraftingRecipe?)_dataProvider?.GetItem(id);
            _currentSelectedRecipe = recipe;
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
