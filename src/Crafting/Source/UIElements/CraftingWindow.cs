namespace Crafting.Source.UIElements
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Requests;

    [Tool]
    [GlobalClass]
    public partial class CraftingWindow : DraggableWindow, IInitializable, IClosable, IRequireServices
    {
        private const string UID = "uid://r4d7lhh2ta5x";

        [Export] private Button? _close, _add;
        [Export] private RichTextLabel? _description;
        [Export] private Label? _itemName, _itemUpgradeLevel;
        [Export] private VBoxContainer? _requirements, _additionalStats, _baseStats;
        [Export] private HBoxContainer? _buttons;
        [Export] private TextureRect? _itemIcon;

        private IItemDataProvider? _dataProvider;
        private IInventory? _inventory;
        private IUiMediator? _uiMediator;
        private ISystemMediator? _systemMediator;
        private IItemUpgrader? _itemUpgrader;

        private CraftingMode _craftingMode;

        private Dictionary<string, int> _usedResources = [];
        private string? _id;

        public event Action? Close;

        public override void _Ready()
        {
            if (_close != null)
                _close.Pressed += () => Close?.Invoke();
            if (_add != null)
                _add.Pressed += OnAddPressed;
            if (DragArea != null)
                DragArea.GuiInput += DragWindow;
        }

        public void InjectServices(Core.Interfaces.Data.IServiceProvider provider)
        {
            _dataProvider = provider.GetService<IItemDataProvider>();
            _uiMediator = provider.GetService<IUiMediator>();
            _inventory = provider.GetService<IInventory>();
            _systemMediator = provider.GetService<ISystemMediator>();
            _itemUpgrader = provider.GetService<IItemUpgrader>();
            _uiMediator.UpdateUi += UpdateRequiredResources;
        }

        public void SetRecipe(string recipeId)
        {
            if (recipeId.Equals(_id, StringComparison.OrdinalIgnoreCase)) return;
            RemoveOldData();
            _craftingMode = CraftingMode.Create;
            _id = recipeId;

            var itemId = _dataProvider!.GetRecipeResultItemId(_id);
            var requirements = _dataProvider.GetRecipeRequirements(_id);

            foreach (var req in requirements)
            {
                var reqItem = CreateClickableResource(req.ResourceId, _inventory?.GetTotalItemAmount(req.ResourceId) ?? 0, req.Amount);
                _usedResources.TryAdd(req.ResourceId, req.Amount);
                _requirements?.AddChild(reqItem);
            }
            var create = new ActionButton();
            create.SetupNormalButton(Lokalizator.Lokalize("CraftingCreateButton"), () => CreateItem(recipeId), () => IsEnoughtResources(requirements));
            _buttons?.AddChild(create);
            SetPossibleBaseStats();
            SetPossibleModifiers();
            SetDisplayableData();
        }


        public void SetItem(string itemId)
        {
            if (itemId.Equals(_id, StringComparison.OrdinalIgnoreCase)) return;
            RemoveOldData();
            _craftingMode = CraftingMode.Upgrade;
            _id = itemId;

            SetDisplayableData();
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        public override void _ExitTree()
        {
            if (_uiMediator != null) _uiMediator.UpdateUi -= UpdateRequiredResources;
        }

        private async void OnAddPressed()
        {
            if (_uiMediator == null) return;
            var takenResources = await _uiMediator.Send<OpenCraftingItemsWindowRequest, IEnumerable<string>>(new(_usedResources.Keys));
            AddOptionalResources(takenResources);
            foreach (var taken in takenResources)
                _usedResources.TryAdd(taken, 1);
        }

        private void AddOptionalResources(IEnumerable<string> takenResources)
        {
            foreach (var res in takenResources)
            {
                if (_usedResources.TryAdd(res, 1))
                {
                    var reqIem = CreateClickableResource(res, _inventory?.GetTotalItemAmount(res) ?? 0, 1);
                    reqIem.SetRightClickAction(() =>
                    {
                        _usedResources.Remove(res);
                        reqIem.QueueFree();
                    });
                    reqIem.SetClickable(true);
                    _requirements?.AddChild(reqIem);
                }
            }
        }

        private void UpdateRequiredResources()
        {
            foreach (var resource in _requirements?.GetChildren().Cast<ClickableResource>() ?? [])
            {
                var resourceId = resource.GetResourceId();
                _usedResources.TryGetValue(resourceId, out var need);
                resource.SetText(Lokalizator.Lokalize(resourceId), _inventory?.GetTotalItemAmount(resourceId) ?? 0, need);
            }
        }

        private void SetPossibleBaseStats()
        {
            if (_dataProvider == null || string.IsNullOrWhiteSpace(_id)) return;

            var resultItemId = _dataProvider.GetRecipeResultItemId(_id);
            var itemId = _dataProvider.IsItemHasTag(_id, "Generic") ? $"{resultItemId}_Generic" : resultItemId;
            var itemBaseStats = _dataProvider.GetItemBaseStats(itemId).ToHashSet();
            CreateHoverArea(itemBaseStats, _baseStats);
        }


        private void SetPossibleModifiers() => CreateHoverArea([], _additionalStats, true);

        private void CreateHoverArea(HashSet<IModifier> modifier, BoxContainer? areaContainer, bool updatable = false)
        {
            var label = new Label
            {
                LabelSettings = new LabelSettings()
                {
                    FontSize = 14,
                },
                Text = "1-4"
            };

            var boxContainer = new HBoxContainer()
            {
                Alignment = BoxContainer.AlignmentMode.Center,
            };

            var hover = new HoverableItem();
            if (updatable)
                hover.SetFuncToUpdateModifiers(GetModifiersUsedResources);
            else
                hover.SetModifiersToShow(modifier);

            boxContainer.AddChild(label);
            boxContainer.AddChild(hover);
            areaContainer?.AddChild(boxContainer);
        }


        private HashSet<IModifier> GetModifiersUsedResources()
        {
            var possibleModifiers = new List<IModifier>();

            foreach (var res in _usedResources)
                possibleModifiers.AddRange(_dataProvider?.GetResourceModifiers(res.Key) ?? []);

            return [.. possibleModifiers];
        }

        private void CreateItem(string id)
        {
            var requrements = _dataProvider?.GetRecipeRequirements(id);
            var itemId = _dataProvider?.GetRecipeResultItemId(id);
            var modifiers = new List<IMaterialModifier>();
            foreach (var req in requrements ?? [])
                modifiers.AddRange(_dataProvider?.GetResourceModifiers(req.ResourceId) ?? []);

            _uiMediator?.Send(new CreateEquipItemRequest(itemId ?? string.Empty, modifiers, _usedResources));
        }

        private void SetDisplayableData()
        {
            _itemName.Text = Lokalizator.Lokalize(_id ?? string.Empty);
            _description.Text = Lokalizator.LokalizeDescription(_id ?? string.Empty);
            _itemIcon.Texture = _dataProvider?.GetItemIcon(_id ?? string.Empty);
        }


        private ClickableResource CreateClickableResource(string resourceId, int have, int need)
        {
            var reqItem = ClickableResource.Initialize().Instantiate<ClickableResource>();
            reqItem.SetResourceId(resourceId);
            reqItem.SetIcon(_dataProvider?.GetItemIcon(resourceId));
            reqItem.SetText(Lokalizator.Lokalize(resourceId), have, need);

            return reqItem;
        }

        private void FreeChildren(Godot.Collections.Array<Node> children)
        {
            foreach (var child in children)
                child.QueueFree();
        }

        private void RemoveOldData()
        {
            FreeChildren(_requirements?.GetChildren() ?? []);
            FreeChildren(_buttons?.GetChildren() ?? []);
            FreeChildren(_baseStats?.GetChildren() ?? []);
            FreeChildren(_additionalStats?.GetChildren() ?? []);
            _usedResources.Clear();
        }

        private bool IsEnoughtResources(IEnumerable<IResourceRequirement> requirements) => !requirements.Any(res => _inventory?.GetTotalItemAmount(res.ResourceId) < res.Amount);
    }
}
