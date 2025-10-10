namespace Crafting.Source.UIElements.Layers
{
    using Godot;
    using Utilities;
    using Core.Interfaces.Items;
    using Crafting.TestResources;
    using Crafting.TestResources.DI;
    using Crafting.TestResources.Inventory;

    public partial class UILayer : CanvasLayer
    {
        private UIElementProvider? _uIElementProvider;
        private ItemInventory? _itemInventory;
        private ItemDataProvider? _itemDataProvider;
        private Node? _shownItem;

        public override void _Ready()
        {
            _uIElementProvider = ServiceProvider.Instance.GetService<UIElementProvider>();
            _itemInventory = ServiceProvider.Instance.GetService<ItemInventory>();
            _itemDataProvider = ServiceProvider.Instance.GetService<ItemDataProvider>();
            EventBus.ItemCreated += OnItemCreated;
            EventBus.MouseEnterInventorySlot += OnMouseEnterInventorySlot;
            EventBus.MouseExitInventorySlot += OnMouseExitInventorySlot;
        }
      
        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_crafting"))
            {
                if (Visible) Hide();
                else Show();
            }
        }

        private void OnItemCreated(IItem item)
        {
            var inventory = ServiceProvider.Instance.GetService<ItemInventory>();
            var notifier = _uIElementProvider!.CreateSingleClosableOrGet<ItemCreatedNotifier>(this);
            notifier.OkPressed += () =>
            {
                _itemInventory?.TryAddItem(item);
            };
            notifier.DestroyPressed += () =>
            {
                DestroyItem(item);
            };

            if (item is IEquipItem equip)
            {
                var itemDetails = CreateEquipItemDetails(equip);
                notifier.SetItemDetails(itemDetails);
            }
        }

        private void ShowEquipItem(IEquipItem equip)
        {
            var itemDetails = CreateEquipItemDetails(equip);
            itemDetails.Position = GetViewport().GetMousePosition();
            _shownItem = itemDetails;
            CallDeferred(MethodName.AddChild, _shownItem);
        }

        private ItemDetails CreateEquipItemDetails(IEquipItem equip)
        {
            var itemDetails = ItemDetails.Initialize().Instantiate<ItemDetails>();

            itemDetails.SetItemIcon(equip.Icon!);
            itemDetails.SetItemName(equip.DisplayName);
            itemDetails.SetItemUpdateLevel(equip.UpdateLevel);
            foreach (var item in equip.BaseModifiers)
            {
                var selectable = new SelectableItem();
                selectable.SetSelectable(false);
                selectable.SetText(Lokalizator.Format(item));
                itemDetails.SetItemBaseStats(selectable);
            }

            foreach (var modifier in equip.AdditionalModifiers)
            {
                var stat = new SelectableItem();
                stat.SetText(Lokalizator.Format(modifier));
                stat.SetSelectable(false);
                itemDetails.SetItemAdditionalStats(stat);
            }

            if (equip.Skill != null)
            {
                var skill = equip.Skill;
                var skillDescription = SkillDescription.Initialize().Instantiate<SkillDescription>();
                skillDescription.SetSkillName(skill.DisplayName);
                skillDescription.SetSkillDescription(skill.Description);
                itemDetails.SetSkillDescription(skillDescription);
            }

            return itemDetails;
        }

        private void DestroyItem(IItem item)
        {
            if (item is IEquipItem equip)
            {
                using var rnd = new RandomNumberGenerator();
                rnd.Randomize();
                foreach (var res in equip.UsedResources)
                {
                    var amount = rnd.RandiRange(0, res.Value);
                    if (amount == 0) continue;
                    if (!_itemInventory?.TryAddItemStacks(res.Key, amount) ?? false)
                    {
                        var itemInstance = _itemDataProvider?.CopyBaseItem(res.Key);
                        if (itemInstance != null) _itemInventory?.TryAddItem(itemInstance, amount);
                    }
                }
            }
        }

        private void OnMouseExitInventorySlot()
        {
            if (_shownItem != null)
            {
                _shownItem.QueueFree();
                _shownItem = null;
            }
        }

        private void OnMouseEnterInventorySlot(ItemInstance instance)
        {
            var item = _itemInventory?.GetItem(instance.InstanceId);
            switch (true)
            {
                case var _ when item is IEquipItem equip:
                    ShowEquipItem(equip);
                    break;
            }
        }

    }
}
