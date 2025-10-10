namespace Crafting.Source.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Data;
    using Core.Interfaces.UI;
    using Crafting.TestResources.Inventory;
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
        private ItemInventory? _inventory;
        private IItemCreator? _itemCreator;

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
            _inventory = provider.GetService<ItemInventory>();
            _itemCreator = provider.GetService<IItemCreator>();
        }

        public void SetRecipeId(string recipeId)
        {
            if (recipeId.Equals(_id, StringComparison.OrdinalIgnoreCase)) return;
            foreach (var child in _requirements?.GetChildren() ?? [])
                child.QueueFree();
            foreach(var child in _buttons?.GetChildren() ?? [])
                child.QueueFree();
            _id = recipeId;

            var itemId = _dataProvider!.GetRecipeResultItemId(_id);
            var requirements = _dataProvider.GetRecipeRequirements(_id);

            foreach (var req in requirements)
            {
                var reqItem = ClickableResource.Initialize().Instantiate<ClickableResource>();
                reqItem.SetMetadata(req.ResourceId);
                reqItem.SetIcon(_dataProvider.GetItemIcon(req.ResourceId));
                reqItem.SetText(Lokalizator.Lokalize(req.ResourceId), _inventory?.GetTotalItemAmount(req.ResourceId) ?? 0, req.Amount);
                _requirements?.AddChild(reqItem);
            }
            _itemName.Text = Lokalizator.Lokalize(itemId);
            _description.Text = Lokalizator.LokalizeDescription(itemId);
            _itemIcon.Texture = _dataProvider.GetItemIcon(itemId);

            var create = new ActionButton();
            create.SetupNormalButton(Lokalizator.Lokalize("CraftingCreateButton"), () => CreateItem(recipeId), () => IsEnoughtResources(requirements));
            _buttons?.AddChild(create);
        }

        private void CreateItem(string id)
        {
            var requrements = _dataProvider?.GetRecipeRequirements(id);
            var itemId = _dataProvider?.GetRecipeResultItemId(id);
            var modifiers = new List<IMaterialModifier>();
            foreach (var req in requrements ?? [])
                modifiers.AddRange(_dataProvider?.GetResourceModifiers(req.ResourceId) ?? []);
            var item = _itemCreator?.CreateEquipItem(itemId ?? string.Empty, modifiers);
            if (item != null)
            {
                requrements?.ForEach(req => _inventory?.RemoveItemById(req.ResourceId, req.Amount));
                item.SaveUsedResources(requrements?.ToDictionary(x => x.ResourceId, x => x.Amount) ?? []);
                item.SaveModifiersPool(modifiers);
            }

            UpdateResourceQuantity();
        }

        private void UpdateResourceQuantity()
        {
            var requrements = _dataProvider?.GetRecipeRequirements(_id ?? string.Empty);
            foreach (var item in _requirements?.GetChildren().Cast<ClickableResource>() ?? [])
            {
                var resourceId = item.GetMetadata().AsString();
                item.SetText(Lokalizator.Lokalize(resourceId), _inventory?.GetTotalItemAmount(resourceId) ?? 0, requrements?.Where(x => x.ResourceId == resourceId).First().Amount ?? 0);
            }
        }
        private bool IsEnoughtResources(IEnumerable<IResourceRequirement> requirements) => !requirements.Any(res => _inventory?.GetTotalItemAmount(res.ResourceId) < res.Amount);

        public void SetItemId(string itemId)
        {
            _id = itemId;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);
    }
}
