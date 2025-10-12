namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Data;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Requests;
    using Core.Interfaces.UI;
    using Godot;
    using Utilities;

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

        private ItemDataProvider? _dataProvider;
        private IInventory? _inventory;
        private IMediator? _mediator;
        private Dictionary<string, int> _usedResources = [];
        private string? _id;

        public event Action? Close;

        public override void _Ready()
        {
            if (_close != null)
                _close.Pressed += () => Close?.Invoke();
            if (DragArea != null)
                DragArea.GuiInput += DragWindow;
        }

        public void InjectServices(Core.Interfaces.Data.IServiceProvider provider)
        {
            _dataProvider = provider.GetService<ItemDataProvider>();
            _mediator = provider.GetService<IUiMediator>();
            _inventory = provider.GetService<IInventory>();
        }

        public void SetRecipe(string recipeId)
        {
            if (recipeId.Equals(_id, StringComparison.OrdinalIgnoreCase)) return;
            FreeChildren(_requirements?.GetChildren() ?? []);
            FreeChildren(_buttons?.GetChildren() ?? []);
            FreeChildren(_baseStats?.GetChildren() ?? []);
            FreeChildren(_additionalStats?.GetChildren() ?? []);
            _usedResources.Clear();
            _id = recipeId;

            var itemId = _dataProvider!.GetRecipeResultItemId(_id);
            var requirements = _dataProvider.GetRecipeRequirements(_id);

            foreach (var req in requirements)
            {
                var reqItem = ClickableResource.Initialize().Instantiate<ClickableResource>();
                reqItem.SetMetadata(req.ResourceId);
                reqItem.SetIcon(_dataProvider.GetItemIcon(req.ResourceId));
                reqItem.SetText(Lokalizator.Lokalize(req.ResourceId), _inventory?.GetTotalItemAmount(req.ResourceId) ?? 0, req.Amount);
                _usedResources.TryAdd(req.ResourceId, req.Amount);
                _requirements?.AddChild(reqItem);
            }
            _itemName.Text = Lokalizator.Lokalize(itemId);
            _description.Text = Lokalizator.LokalizeDescription(itemId);
            _itemIcon.Texture = _dataProvider.GetItemIcon(itemId);

            var create = new ActionButton();
            create.SetupNormalButton(Lokalizator.Lokalize("CraftingCreateButton"), () => CreateItem(recipeId), () => IsEnoughtResources(requirements));
            _buttons?.AddChild(create);
            SetBaseStats();
            SetPossibleModifiers();
        }

        public void SetItemId(string itemId)
        {
            _id = itemId;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void SetBaseStats()
        {
            var itemId = _dataProvider!.GetRecipeResultItemId(_id ?? string.Empty);
            var itemBaseStats = _dataProvider.GetItemBaseStats(itemId);

            var container = CreatePopup(itemBaseStats);
            _baseStats?.AddChild(container);
        }

        private void SetPossibleModifiers()
        {
            var possibleModifiers = new List<IMaterialModifier>();

            foreach (var res in _usedResources)
                possibleModifiers.AddRange(_dataProvider?.GetResourceModifiers(res.Key) ?? []);

            var container = CreatePopup(possibleModifiers);
            _additionalStats?.AddChild(container);
        }

        private HBoxContainer CreatePopup<T>(List<T> modifiers)
            where T : IModifier
        {
            var popUp = new PopupWindow();
            popUp.Setup();
            foreach (var stat in modifiers)
            {
                var text = new Label
                {
                    Text = Lokalizator.Format(stat),
                    LabelSettings = new LabelSettings()
                    {
                        FontSize = 12,
                    }
                };
                popUp.AddItem(text);
            }

            var container = new HBoxContainer()
            {
                Alignment = BoxContainer.AlignmentMode.Center
            };

            var label = new Label()
            {
                Text = "1-4",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                LabelSettings = new LabelSettings()
                {
                    FontSize = 14
                }
            };
            var hoverable = new HoverableItem();
            hoverable.Setup();

            container.AddChild(label);
            container.AddChild(hoverable);

            return container;
        }

        private void CreateItem(string id)
        {
            var requrements = _dataProvider?.GetRecipeRequirements(id);
            var itemId = _dataProvider?.GetRecipeResultItemId(id);
            var modifiers = new List<IMaterialModifier>();
            foreach (var req in requrements ?? [])
                modifiers.AddRange(_dataProvider?.GetResourceModifiers(req.ResourceId) ?? []);

            _mediator?.Send(new CreateEquipItemRequest(itemId ?? string.Empty, modifiers, _usedResources));
        }

        private void FreeChildren(Godot.Collections.Array<Node> children)
        {
            foreach (var child in children)
                child.QueueFree();
        }

        private bool IsEnoughtResources(IEnumerable<IResourceRequirement> requirements) => !requirements.Any(res => _inventory?.GetTotalItemAmount(res.ResourceId) < res.Amount);
    }
}
