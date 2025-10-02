namespace Crafting.Source.UIElements.Layers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Core.Constants;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Results;
    using Crafting.TestResources.Inventory;
    using Godot;
    using Stateless;
    using Utilities;

    public partial class CraftingLayer : CanvasLayer
    {
        private enum Trigger { Open, Close, SetRecipe, SetEquip }
        private enum State { Opened, Closed, Recipe, Equip }

        private readonly StateMachine<State, Trigger> _machine = new(State.Closed);

        private readonly HashSet<IMaterialModifier> _mainModifiers = [];
        private readonly HashSet<IMaterialModifier> _optionalModifiers = [];
        // optional quantity for optional resource always 1
        private readonly HashSet<string> _optionalResources = [];
        private readonly Dictionary<string, int> _mainResources = [];

        private StateMachine<State, Trigger>.TriggerWithParameters<string>? _recipeSelected;
        private StateMachine<State, Trigger>.TriggerWithParameters<string>? _equipItemSelected;

        private readonly ItemDataProvider _dataProvider = new("res://TestResources/RecipeAndResources/");
        private readonly ItemCreator _itemCreator = new();
        private readonly ItemUpgrader _itemUpgrader = new();
        private readonly ItemInventory _itemInventory = new();
        [Export] private CraftingUI? _craftingUI;

        public override void _Ready()
        {
            // TODO: Remember upgrade to Mythic item have special requirements
            _dataProvider.LoadData();
            ConfigureMachine();

            // for now
            // ____________________________________________
            _craftingUI?.InitializeInventory(_itemInventory);

            using (var rnd = new RandomNumberGenerator())
                foreach (var resource in _dataProvider.GetAllResources())
                {
                    _itemInventory.TryAddItem(resource, 100);
                }
            // ____________________________________________

            if (_craftingUI != null)
            {
                _craftingUI.RecipeSelected += (id) => _machine.Fire(_recipeSelected, id);
                _craftingUI.OnEquipItemPlaced += (instanceId) => _machine.Fire(_equipItemSelected, instanceId);
                _craftingUI.OnEquipItemReturned += (id, instance) => _itemInventory.TryReturnItemInstanceToInventory(new(id, instance));
                _craftingUI.OnEquipItemRemoved += PrepareUI;
                _craftingUI.ModifierSelected += ChangeSelectedModifier;
            }

            for (int i = 0; i < 4; i++)
            {
                var opt = OptionalResource.Initialize().Instantiate<OptionalResource>();
                opt.ResourceRemoved += OnResourceRemoved;
                opt.AddPressed += () => OnAddPressed(opt);
                _craftingUI?.AddOptionalResource(opt);
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_crafting")) FireTrigger(Visible ? Trigger.Close : Trigger.Open);
        }

        private void ConfigureMachine()
        {
            _recipeSelected = _machine.SetTriggerParameters<string>(Trigger.SetRecipe);
            _equipItemSelected = _machine.SetTriggerParameters<string>(Trigger.SetEquip);

            _machine.Configure(State.Closed)
                .OnEntry(() =>
                {
                    Hide();
                    ReturnItemInCraftingSlotIfNeeded();
                    _mainModifiers.Clear();
                    _optionalModifiers.Clear();
                    ClearOptionalResources();
                    _craftingUI?.DestroyRecipeTree();
                    _craftingUI?.ClearMainResources();
                    _craftingUI?.ClearDescription();
                    _craftingUI?.ClearItemBaseStats();
                    _craftingUI?.ClearPossibleModifiers();
                    _craftingUI?.ClearItemModifiers();
                })
                .Permit(Trigger.Open, State.Opened);

            _machine.Configure(State.Opened)
                .OnEntry(() =>
                {
                    _craftingUI?.CreatingRecipeTree(_dataProvider.GetCraftingRecipes());
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


        #region CREATE
        private void SetRecipe(string id)
        {
            _craftingUI?.ShowRecipe(id);

            var itemId = _dataProvider.GetRecipeResultItemId(id);
            var requirements = _dataProvider.GetRecipeRequirements(id);

            HandleRequirements(requirements);
            ShowBaseItemStats(itemId);

            _craftingUI?.SetItemDescription(new RichTextLabel() { Text = Lokalizator.LokalizeDescription(itemId), SizeFlagsVertical = Control.SizeFlags.ExpandFill });
            _craftingUI?.ShowPossibleModifiers(FormattedModifiers());

            var button = new ActionButton();
            button.SetupNormalButton(Lokalizator.Lokalize("CraftingCreateButton"), () => OnCreateItem(id), () => IsEnoughtResources(_mainResources));
            _craftingUI?.AddActionNode(button);
        }
       

        private IItem? CreateItem(string recipeId)
        {
            IItem? item = null;
            var resultItemId = _dataProvider.GetRecipeResultItemId(recipeId);
            var hasTag = _dataProvider.IsItemHasTag;
            if (string.IsNullOrWhiteSpace(resultItemId)) return item;

            switch (true)
            {
                // TODO: Creation for generic items not working, i have no resources defined
                case var _ when hasTag.Invoke(recipeId, TagConstants.Equipment):
                    item = hasTag.Invoke(recipeId, "Generic") ?
                        _itemCreator.CreateGenericItem(resultItemId, GetCraftingModifiers()) :
                        _itemCreator.CreateEquipItem(resultItemId, GetCraftingModifiers());
                    break;
                case var _ when hasTag.Invoke(recipeId, TagConstants.Equipment):
                    item = _itemCreator.CreateItem(resultItemId);
                    break;
            }

            if (item is IEquipItem equip)
                SaveUsedResources(equip);
            return item;
        }

        private void SaveUsedResources(IEquipItem equip)
        {
            var resources = new Dictionary<string, int>(_mainResources);
            foreach (var res in _optionalResources)
                resources.TryAdd(res, 1);
            equip.SaveUsedResources(resources);
        }
        #endregion


        #region UPDATE
        private void UpgradeModeNormal(bool isToggled) => SetUpgradeMode(isToggled ? ItemUpgradeMode.Normal : ItemUpgradeMode.None);
        private void UpgradeModeDouble(bool isToggled) => SetUpgradeMode(isToggled ? ItemUpgradeMode.Double : ItemUpgradeMode.None);
        private void UpgradeModeLucky(bool isToggled) => SetUpgradeMode(isToggled ? ItemUpgradeMode.Lucky : ItemUpgradeMode.None);

        private void SetUpgradeMode(ItemUpgradeMode mode)
        {
            var requirements = _itemUpgrader.SetCost(_craftingUI?.GetCraftingSlotItem()?.ItemId ?? string.Empty, mode);
            HandleRequirements(requirements);
        }

        private void UpdateItemButton(string itemInstanceId)
        {
            var item = _itemInventory.GetItemInstance(itemInstanceId);

            // TODO: For now return
            if (item == null) return;
            if (item is not IEquipItem equip) return;
            // ______________________________________

            // TODO: Depends on result we have different sound/visual effect/etc.
            var result = _itemUpgrader.TryUpgradeItem(equip);

            if (!HandleUpdateResult(result))
                return;

            ShowBaseItemStats(equip.InstanceId);
            ShowItemModifiers(equip.AdditionalModifiers);
            ConsumeResourcesFromInventory();
        }

        private bool HandleUpdateResult(IResult<ItemUpgradeResult> result)
        {
            switch (result.Info)
            {
                case ItemUpgradeResult.Success:
                    HandleSuccess();
                    return true;
                case ItemUpgradeResult.Failure:
                    HandleFailure();
                    return true;
                case ItemUpgradeResult.CriticalSuccess:
                    HandleCriticalSuccess();
                    return true;
                case ItemUpgradeResult.CriticalFailure:
                    HandleCriticalFailure();
                    return true;
                case ItemUpgradeResult.UpgradeModeNotSet:
                    return false;
                case ItemUpgradeResult.ReachedMaxLevel:
                    return false;

                default:
                    return false;
            }
        }

        private void HandleCriticalFailure() => throw new NotImplementedException();
        private void HandleCriticalSuccess() => throw new NotImplementedException();
        private void HandleFailure() => throw new NotImplementedException();
        private void HandleSuccess() => throw new NotImplementedException();


        private void AllowRecraftModifier(bool isToggled)
        {
            bool isSelectable = false;

            if (isToggled)
            {
                // for now
                var item = _itemInventory.GetItemInstance(_craftingUI?.GetCraftingSlotItem()?.InstanceId ?? string.Empty) as IEquipItem;
                var reqs = _itemUpgrader.GetRecraftResourceRequirements($"{item?.EquipmentPart.ConvertEquipmentPartToCategory()}_{item?.Rarity}");
                HandleRequirements(reqs);
                isSelectable = IsEnoughtResources(_mainResources);
            }
            else
            {
                _craftingUI?.ClearMainResources();
                ClearOptionalResources();
            }

            _craftingUI?.SetItemModifiersSelectable(isSelectable);
        }

        private void ChangeSelectedModifier(int hash, string itemInstanceId)
        {
            var item = _itemInventory.GetItemInstance(itemInstanceId);
            // TODO: For now return
            if (item == null) return;
            if (item is not IEquipItem equip) return;

            ConsumeResourcesFromInventory();
            var newModifier = _itemUpgrader.TryRecraftModifier(equip, hash, equip.ModifiersPool.Concat(_optionalModifiers));

            _craftingUI?.UpdateSelectedItemModifer((Lokalizator.Format(newModifier), newModifier.GetHashCode()));

        }
        #endregion

        private void SetEquipItem(string itemId)
        {
            _craftingUI?.DeselecteAllRecipe();

            var item = _itemInventory.GetItemInstance(itemId);
            if (item != null && item is IEquipItem equip)
            {
                ShowBaseItemStats(equip.InstanceId);
                ShowItemModifiers(equip.AdditionalModifiers);
                CreateActionButtons(equip.InstanceId);
            }
        }

        private void ShowMainResources()
        {
            foreach (var res in _mainResources)
            {
                var id = res.Key;
                var amountNeed = GetAmountNeed(res);
                var mainRes = MainResource.Initialize().Instantiate<MainResource>();
                mainRes.AddCraftingResource(id, res => _itemInventory?.GetTotalItemAmount(id) ?? 0, amountNeed);
                _craftingUI?.AddMainResource(mainRes);
                AddModifiersIfResource(id);
            }
        }

        private void ShowItemModifiers(IReadOnlyList<IItemModifier> modifiers)
        {
            var mods = new List<(string Modifier, int Hash)>();
            foreach (var mod in modifiers)
            {
                mods.Add((Lokalizator.Format(mod), mod.GetHashCode()));
            }
            _craftingUI?.SetItemModifiers(mods);
        }

        //TODO: Rework
        private void ShowBaseItemStats(string itemId)
        {
            if (_machine.State == State.Recipe)
            {
                var stats = _dataProvider.GetItemBaseStats(itemId);
                string baseStats = $"{Lokalizator.Lokalize("BaseStats")}\n";
                foreach (var item in stats ?? [])
                {
                    baseStats += $"{item}\n";
                }
                _craftingUI?.SetBaseStats(baseStats);
            }
            else
            {
                var item = _itemInventory.GetItemInstance(itemId) as IEquipItem;
                string symbol = item?.UpdateLevel > 0 ? ": +" : string.Empty;
                string currentLevel = item?.UpdateLevel > 0 ? $"{item.UpdateLevel}" : string.Empty;
                string baseStats = $"{Lokalizator.Lokalize(item?.Id ?? string.Empty)}{symbol}{currentLevel}\n";
                foreach (var stat in item?.BaseModifiers ?? [])
                {
                    baseStats += $"{Lokalizator.Format(stat)}\n";
                }
                _craftingUI?.SetBaseStats(baseStats);
            }
        }

        private void HandleRequirements(List<IResourceRequirement> requrements)
        {
            _mainResources.Clear();
            _craftingUI?.ClearMainResources();
            requrements.ForEach(req => _mainResources.TryAdd(req.ResourceId, req.Amount));
            ShowMainResources();
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
                _itemInventory.TryAddItem(item);
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

        private void CreateActionButtons(string itemInstanceId)
        {
            // TODO: позднее убрать кнопку апдейта и добавить функцию апгрейда предмета путем нажатия на окошко характеристик предмета
            var buttonGroup = new ButtonGroup
            {
                AllowUnpress = true
            };
            var normal = new ActionButton();
            var doubl = new ActionButton();
            var lucky = new ActionButton();
            var update = new ActionButton();
            var recraft = new ActionButton();

            normal.SetupToggleButton(Lokalizator.Lokalize("UpdateNormal"), UpgradeModeNormal, buttonGroup);
            doubl.SetupToggleButton(Lokalizator.Lokalize("UpdateDouble"), UpgradeModeDouble, buttonGroup);
            lucky.SetupToggleButton(Lokalizator.Lokalize("UpdateRisk"), UpgradeModeLucky, buttonGroup);
            update.SetupNormalButton(Lokalizator.Lokalize("CraftingUpdateButton"), () => UpdateItemButton(itemInstanceId), () => IsEnoughtResources(_mainResources));
            recraft.SetupToggleButton(Lokalizator.Lokalize("RecraftModifiers"), AllowRecraftModifier, buttonGroup);

            _craftingUI?.AddActionNode(normal);
            _craftingUI?.AddActionNode(doubl);
            _craftingUI?.AddActionNode(lucky);
            _craftingUI?.AddActionNode(update);
            _craftingUI?.AddActionNode(recraft);
        }
        // ______________________________

        private void ConsumeResourcesFromInventory()
        {
            // TODO: handle optional resources
            foreach (var res in _mainResources)
                ConsumeSingleResourceFromInventory(res.Key, res.Value);
            foreach (var optResourceId in _optionalResources)
                ConsumeSingleResourceFromInventory(optResourceId);

            _craftingUI?.ConsumeOptionalResource();
            _craftingUI?.ConsumeMainResources();
        }

        #region EVENTS

        private void OnResourceAdded(string resource)
        {
            var modifiers = _dataProvider.GetResourceModifiers(resource);
            AddModifiersToSet(_optionalModifiers, modifiers);
            _craftingUI?.ShowPossibleModifiers(FormattedModifiers());
        }

        private void OnResourceRemoved(string resource)
        {
            var modifiers = _dataProvider.GetResourceModifiers(resource);
            RemoveModifiersFromSet(_optionalModifiers, modifiers);
            _optionalResources.RemoveWhere(res => res == resource);
            _craftingUI?.ShowPossibleModifiers(FormattedModifiers());
        }

        private void OnCreateItem(string id)
        {
            var item = CreateItem(id);
            if (item == null)
            {
                Tracker.TrackError($"Failed to create an item. Recipe: {id}", this);
                return;
            }
            CreateNotifier(item);
            ConsumeResourcesFromInventory();
        }

        private void OnLaguageChanged()
        {
            var currentLang = TranslationServer.GetLocale();
            if (currentLang == "en_GB") TranslationServer.SetLocale("ru");
            else TranslationServer.SetLocale("en_GB");
        }

        private void OnAddPressed(OptionalResource opt)
        {
            var available = _itemInventory.GetAllItemIdsWithTag(TagConstants.Essence) ?? [];

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
        #endregion


        private int GetResourceAmount(int value)
        {
            // TODO: Need to call player to get multiplier
            var playerMultiplier = 0.3f;

            return Mathf.Max(0, Mathf.RoundToInt(value * playerMultiplier));
        }

        private List<IMaterialModifier> GetCraftingModifiers()
        {
            var resources = new List<IMaterialModifier>();
            foreach (var res in _optionalResources.Concat(_mainResources.Keys))
            {
                var loadded = _dataProvider.GetResourceModifiers(res);
                resources.AddRange(loadded);
            }

            return resources;
        }

        private void ReturnItemInCraftingSlotIfNeeded()
        {
            _itemInventory.TryReturnItemInstanceToInventory(_craftingUI?.GetCraftingSlotItem());
            _craftingUI?.ClearCraftingSlot();
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

        private void AddModifiersIfResource(string id)
        {
            if (_dataProvider?.IsItemImplement<ICraftingResource>(id) ?? false) AddModifiersToSet(_mainModifiers, _dataProvider.GetResourceModifiers(id));
        }

        private void ClearOptionalResources()
        {
            _optionalResources.Clear();
            _craftingUI?.ClearOptionalResources();
        }

        // TODO: Maybe I'll have a skill that changes the amount?
        private int GetAmountNeed(KeyValuePair<string, int> res) => res.Value;

        private void ConsumeSingleResourceFromInventory(string resourceId, int amount = 1) => _itemInventory.RemoveItemById(resourceId, amount);

        private void FireTrigger(Trigger trigger) => _machine.Fire(trigger);

        private bool IsEnoughtResources(Dictionary<string, int> resources) => !resources.Any(res => _itemInventory.GetTotalItemAmount(res.Key) < res.Value);

        private HashSet<IMaterialModifier> ConcatModifierSets() => [.. _mainModifiers, .. _optionalModifiers];

        private void DestroyItem(IItem item)
        {
            switch (item)
            {
                case var _ when item is IEquipItem eq:
                    DestroyEquipItem(eq);
                    break;

                default:
                    break;
            }
        }

        private void DestroyEquipItem(IEquipItem equip)
        {
            foreach (var res in equip.UsedResources)
            {
                var amount = GetResourceAmount(res.Value);
                if (amount == 0) continue;
                if (!_itemInventory?.TryAddItemStacks(res.Key, amount) ?? false)
                {
                    var itemInstance = _dataProvider.CopyBaseItem(res.Key);
                    if (itemInstance != null) _itemInventory?.TryAddItem(itemInstance, amount);
                }
            }
        }

        private string FormattedModifiers()
        {
            var formatted = new StringBuilder();
            foreach (var mod in ConcatModifierSets())
                formatted.AppendLine(Lokalizator.Format(mod));
            return formatted.ToString();
        }

        private void PrepareUI()
        {
            _mainModifiers.Clear();
            _craftingUI?.ClearItemBaseStats();
            _craftingUI?.ClearMainResources();
            _craftingUI?.ClearItemModifiers();
            _craftingUI?.ClearDescription();
            _craftingUI?.ShowPossibleModifiers(FormattedModifiers());
        }
    }
}
