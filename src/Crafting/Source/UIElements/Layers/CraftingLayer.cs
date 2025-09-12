namespace Crafting.Source.UIElements.Layers
{
    using Godot;
    using System;
    using Utilities;
    using System.Linq;
    using Core.Constants;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;
    using Crafting.TestResources.Inventory;
    using Stateless;
    using System.Text;

    public partial class CraftingLayer : CanvasLayer
    {
        private enum Trigger { Open, Close, SetRecipe, SetEquip }
        private enum State { Opened, Closed, Recipe, Equip }

        private readonly StateMachine<State, Trigger> _machine = new(State.Closed);
        private readonly HashSet<IMaterialModifier> _mainModifiers = [];
        private readonly HashSet<IMaterialModifier> _optionalModifiers = [];
        private readonly HashSet<string> _optionalResources = [];
        private readonly HashSet<string> _mainResources = [];

        private StateMachine<State, Trigger>.TriggerWithParameters<string>? _recipeSelected;
        private StateMachine<State, Trigger>.TriggerWithParameters<string>? _equipItemSelected;

        private ItemDataProvider? _dataProvider;
        private ItemCreator? _itemCreator;
        private ItemUpgrader? _itemUpgrader;
        private ItemInventory? _itemInventory;
        [Export] private CraftingUI? _craftingUI;

        public override void _Ready()
        {
            _dataProvider = new("res://TestResources/RecipeAndResources/");
            _dataProvider.LoadData();
            _itemCreator = new ItemCreator();
            _itemUpgrader = new ItemUpgrader();

            ConfigureMachine();
            if (_craftingUI != null)
            {
                _craftingUI.RecipeSelected += (id) => _machine.Fire(_recipeSelected, id);
                _craftingUI.OnEquipItemPlaced += (id) => _machine.Fire(_equipItemSelected, id);
                _craftingUI.OnEquipItemReturned += (id, instance) => _itemInventory?.ReturnItemToInventory(new(id, instance));
                _craftingUI.OnEquipItemRemoved += PrepareUI;
                _craftingUI.ModifierSelected += (hash, instanceId) => GD.Print($"Modifier with hash: {hash}. Have item in inventory: {_itemInventory?.GetItemInstance(instanceId) != null}");
            }

            for (int i = 0; i < 4; i++)
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
        }

        private void ConfigureMachine()
        {
            _recipeSelected = _machine.SetTriggerParameters<string>(Trigger.SetRecipe);
            _equipItemSelected = _machine.SetTriggerParameters<string>(Trigger.SetEquip);

            _machine.Configure(State.Closed)
                .OnEntry(() =>
                {
                    Hide();
                    ReturnCraftingSlotItemIfNeeded();
                    _mainModifiers.Clear();
                    _mainResources.Clear();
                    _optionalModifiers.Clear();
                    _optionalResources.Clear();
                    _craftingUI?.DestroyRecipeTree();
                    _craftingUI?.ClearMainResources();
                    _craftingUI?.ClearDescription();
                    _craftingUI?.ClearItemBaseStats();
                    _craftingUI?.ClearPossibleModifiers();
                    _craftingUI?.ClearItemModifiers();
                    _craftingUI?.ClearOptionalResources();
                })
                .Permit(Trigger.Open, State.Opened);

            _machine.Configure(State.Opened)
                .OnEntry(() =>
                {
                    _craftingUI?.CreatingRecipeTree(_dataProvider?.GetCraftingRecipes() ?? []);
                    Show();
                })
                .Permit(Trigger.SetEquip, State.Equip)
                .Permit(Trigger.SetRecipe, State.Recipe)
                .Permit(Trigger.Close, State.Closed);

            _machine.Configure(State.Recipe)
                .OnEntry(PrepareUI)
                .OnEntryFrom(_recipeSelected, SetRecipe)
                .OnExit(() => { _craftingUI?.ClearActions(); })
                .PermitReentry(Trigger.SetRecipe)
                .Permit(Trigger.Close, State.Closed)
                .Permit(Trigger.SetEquip, State.Equip);

            _machine.Configure(State.Equip)
                .OnEntry(PrepareUI)
                .OnEntryFrom(_equipItemSelected, SetEquipItem)
                .OnExit(() => { _craftingUI?.ClearActions(); })
                .PermitReentry(Trigger.SetEquip)
                .Permit(Trigger.SetRecipe, State.Recipe)
                .Permit(Trigger.Close, State.Closed);
        }

        private void ReturnCraftingSlotItemIfNeeded()
        {
            var itemInstance = _craftingUI?.GetCraftingSlotItem();
            // recipe, dont care just clear it
            if (itemInstance == null || string.IsNullOrWhiteSpace(itemInstance.InstanceId))
            {
                _craftingUI?.ClearCraftingSlot();
                return;
            }
            // crafting slot always have 1 item
            _itemInventory?.ReturnItemToInventory(itemInstance);
            _craftingUI?.ClearCraftingSlot();
        }

        private void SetRecipe(string id)
        {
            _craftingUI?.SetRecipe(id);
            var itemId = _dataProvider?.GetRecipeResultItemId(id) ?? string.Empty;
            ShowBaseItemStats(itemId);
            bool canCraft = ShowMainResources(_dataProvider?.GetRecipeRequirements(id) ?? []);
            _craftingUI?.ShowModifiers(FormattedModifiers());
            _craftingUI?.ClearDescription();
            _craftingUI?.SetItemDescription(new RichTextLabel() { Text = Lokalizator.LokalizeDescription(itemId), SizeFlagsVertical = Control.SizeFlags.ExpandFill });
            var button = new ActionButton();
            button.SetupNormalButton(Lokalizator.Lokalize("CraftingCreateButton"), () => CreateItemButton(id), canCraft);
            _craftingUI?.AddActionNode(button);
        }

        private void SetEquipItem(string itemId)
        {
            _craftingUI?.DeselecteAllRecipe();

            var item = _itemInventory?.GetItemInstance(itemId);
            if (item != null)
            {
                ShowBaseItemStats(item.InstanceId);
                ShowItemModifiers(item);
            }

            // TODO: Remove from here
            var buttonGroup = new ButtonGroup
            {
                AllowUnpress = true
            };
            var normal = new ActionButton();
            normal.SetupToggleButton(Lokalizator.Lokalize("UpdateNormal"), ToggleOne, buttonGroup);
            var doubl = new ActionButton();
            doubl.SetupToggleButton(Lokalizator.Lokalize("UpdateDouble"), ToggleTwo, buttonGroup);
            var risky = new ActionButton();
            risky.SetupToggleButton(Lokalizator.Lokalize("UpdateRisk"), ToggleThree, buttonGroup);
            var update = new ActionButton();
            update.SetupNormalButton(Lokalizator.Lokalize("CraftingUpdateButton"), UpdateItemButton);
            _craftingUI?.AddActionNode(normal);
            _craftingUI?.AddActionNode(doubl);
            _craftingUI?.AddActionNode(risky);
            _craftingUI?.AddActionNode(update);
        }

        private void ToggleOne(bool isToggled)
        {
            if (isToggled)
            {
                _craftingUI?.ClearMainResources();
                var req = _itemUpgrader?.SetCost(_craftingUI?.GetCraftingSlotItem()?.ItemId ?? string.Empty, Core.Enums.ItemUpgradeMode.Normal) ?? [];
                ShowMainResources(req);
            }
        }
        private void ToggleTwo(bool isToggled)
        {
            if (isToggled)
            {
                _craftingUI?.ClearMainResources();
                var req = _itemUpgrader?.SetCost(_craftingUI?.GetCraftingSlotItem()?.ItemId ?? string.Empty, Core.Enums.ItemUpgradeMode.Double) ?? [];
                ShowMainResources(req);
            }
        }
        private void ToggleThree(bool isToggled)
        {
            if (isToggled)
            {
                _craftingUI?.ClearMainResources();
                var req = _itemUpgrader?.SetCost(_craftingUI?.GetCraftingSlotItem()?.ItemId ?? string.Empty, Core.Enums.ItemUpgradeMode.Lucky) ?? [];
                ShowMainResources(req);
            }
        }

        private bool ShowMainResources(List<IResourceRequirement> recipeRequirements)
        {
            bool canCraft = false;
            foreach (var req in recipeRequirements)
            {
                var reqResourceId = req.ResourceId;
                _mainResources.Add(reqResourceId);
                var mainRes = MainResource.Initialize().Instantiate<MainResource>();
                mainRes.AddCraftingResource(reqResourceId, res => _itemInventory?.GetTotalItemAmount(reqResourceId) ?? 0, req.Amount);

                _craftingUI?.AddMainResource(mainRes);
                var amountHave = _itemInventory?.GetTotalItemAmount(reqResourceId) ?? 0;
                canCraft = amountHave >= req.Amount;
                AddModifiersToSet(_mainModifiers, _dataProvider?.GetResourceModifiers(reqResourceId) ?? []);
            }
            return canCraft;
        }



        private void ShowItemModifiers(IItem item)
        {
            if (item is IEquipItem equip)
            {
                var mods = new List<(string, int)>();
                foreach (var mod in equip.AdditionalModifiers)
                {
                    mods.Add((Lokalizator.Format(mod), mod.GetHashCode()));
                }
                _craftingUI?.SetItemModifiers(mods);
            }
        }

        private void FireTrigger(Trigger trigger) => _machine.Fire(trigger);

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_crafting"))
            {
                if (Visible) FireTrigger(Trigger.Close);
                else FireTrigger(Trigger.Open);
            }
        }

        // if we can press this button => we have enough resources
        private void CreateItemButton(string id)
        {
            // What if i need to create item instead of EquipItem??
            var item = CreateItem(id);
            if (item == null)
            {
                Logger.LogError($"Failed to create an item. Recipe: {id}", this);
                return;
            }
            CreateNotifier(item);
            ConsumeResourcesFromInventory(id);
        }

        private void UpdateItemButton()
        {
            var instance = _craftingUI?.GetCraftingSlotItem();
            var item = _itemInventory?.GetItemInstance(instance?.InstanceId ?? string.Empty);
            if (item != null && item is IEquipItem equip) _itemUpgrader?.UpgradeItem(equip);
            ShowBaseItemStats(item?.InstanceId ?? string.Empty);
            ShowItemModifiers(item!);
        }

        // should not be there
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
            var recipe = (ICraftingRecipe?)_dataProvider?.CopyBaseItem(recipeId);
            if (recipe == null) return null;
            switch (true)
            {
                // TODO: Creation for generic items not working, i have no resources
                case var _ when recipe.Tags.Contains(TagConstants.Equipment, StringComparer.OrdinalIgnoreCase):
                    item = recipe.Tags.Contains("Generic") ?
                        _itemCreator?.CreateGenericItem(recipe, GetCraftingResources(_mainResources), GetCraftingResources(_optionalResources)) :
                        _itemCreator?.CreateEquipItem(recipe, GetCraftingResources(_mainResources), GetCraftingResources(_optionalResources));
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

        private void ConsumeResourcesFromInventory(string id)
        {
            foreach (var res in _dataProvider?.GetRecipeRequirements(id) ?? [])
                _itemInventory?.RemoveItemById(res.ResourceId, res.Amount);
            foreach (var optResourceId in _optionalResources)
                _itemInventory?.RemoveItemById(optResourceId);

            _craftingUI?.ConsumeOptionalResource();
            _craftingUI?.ConsumeMainResources();
        }

        private void OnAddPressed(OptionalResource opt)
        {
            var available = _itemInventory?.GetAllItemIdsWithTag(TagConstants.Essence) ?? [];

            var craftingItems = CraftingItems.Initialize().Instantiate<CraftingItems>();
            craftingItems.Setup(available, _optionalResources, selected =>
            {
                opt.AddCraftingResource(selected, id => _itemInventory?.GetTotalItemAmount(id) ?? 0);
                _optionalResources.Add(selected);
                OnResourceAdded(selected);
                craftingItems.QueueFree();
            },
            craftingItems.QueueFree);
            AddChild(craftingItems);
        }

        //TODO: Rework
        private void ShowBaseItemStats(string itemId)
        {
            if (_machine.State == State.Recipe)
            {
                var stats = _dataProvider?.GetItemBaseStats(itemId);
                string baseStats = $"{Lokalizator.Lokalize("BaseStats")}\n";
                foreach (var item in stats ?? [])
                {
                    baseStats += $"{item}\n";
                }
                _craftingUI?.SetBaseStats(baseStats);
            }
            else
            {
                var item = _itemInventory?.GetItemInstance(itemId) as IEquipItem;
                string baseStats = $"{Lokalizator.Lokalize(item?.Id ?? string.Empty)} : {item?.UpdateLevel}\n";
                foreach (var stat in item?.BaseModifiers ?? [])
                {
                    baseStats += $"{Lokalizator.Format(stat)}\n";
                }
                _craftingUI?.SetBaseStats(baseStats);
            }
        }

        private void PrepareUI()
        {
            _mainResources?.Clear();
            _mainModifiers.Clear();
            _craftingUI?.ClearItemBaseStats();
            _craftingUI?.ClearMainResources();
            _craftingUI?.ClearItemModifiers();
            _craftingUI?.ShowModifiers(FormattedModifiers());
            _craftingUI?.ClearDescription();
        }

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
            _optionalResources.RemoveWhere(res => res == resource);
            _craftingUI?.ShowModifiers(FormattedModifiers());
        }

        private void OnLaguageChanged()
        {
            var currentLang = TranslationServer.GetLocale();
            if (currentLang == "en_GB") TranslationServer.SetLocale("ru");
            else TranslationServer.SetLocale("en_GB");
        }

        private void AddModifiersToSet(HashSet<IMaterialModifier> set, IReadOnlyList<IMaterialModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                set.Add(modifier);
        }

        private void RemoveModifiersFromSet(HashSet<IMaterialModifier> set, IReadOnlyList<IMaterialModifier> modifiers)
        {
            foreach (var modifier in modifiers)
                set.Remove(modifier);
        }

        private HashSet<IMaterialModifier> ConcatModifierSets() => [.. _mainModifiers, .. _optionalModifiers];

        private string FormattedModifiers()
        {
            var formatted = new StringBuilder();
            foreach (var mod in ConcatModifierSets())
                formatted.AppendLine(Lokalizator.Format(mod));
            return formatted.ToString();
        }
    }
}
