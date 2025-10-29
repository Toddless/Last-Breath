namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Utilities;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Mediator.Events;

    public class ItemCreatedEventHandler : IEventHandler<ItemCreatedEvent>
    {
        private readonly IUIElementProvider _uIElementProvider;
        private readonly ISystemMediator _systemMediator;
        private readonly IInventory _inventory;

        public ItemCreatedEventHandler(IUIElementProvider provider, IInventory inventory, ISystemMediator systemMediator)
        {
            _uIElementProvider = provider;
            _inventory = inventory;
            _systemMediator = systemMediator;
        }

        public void Handle(ItemCreatedEvent evnt)
        {
            var notifier = _uIElementProvider.CreateAndShowNotification<ItemCreatedNotifier>();
            ConfigureItemCreatedNotifier(evnt.CreatedItem, notifier);

            _inventory.TryAddItem(evnt.CreatedItem);

            notifier.DestroyPressed += OnDestroyPressed;

            void OnDestroyPressed()
            {
                _systemMediator.Publish(new DestroyItemEvent(evnt.CreatedItem.InstanceId));
                notifier.DestroyPressed -= OnDestroyPressed;
            }
        }

        public ItemCreatedNotifier ConfigureItemCreatedNotifier(IItem item, ItemCreatedNotifier instance)
        {
            instance.SetItemDetails(CreateItemDetails(item));
            return instance;
        }

        private ItemDetails CreateItemDetails(IItem item)
        {
            var itemDetails = _uIElementProvider.CreateRequireServices<ItemDetails>();

            if (item is IEquipItem equip)
                return ConfigureEquipItemDetails(itemDetails, equip);

            return ConfigureItemDetails(itemDetails, item);
        }

        private ItemDetails ConfigureItemDetails(ItemDetails itemDetails, IItem item)
        {
            itemDetails.Clear();
            itemDetails.SetItemIcon(item.Icon!);
            itemDetails.SetItemName(item.DisplayName);

            return itemDetails;
        }

        private ItemDetails ConfigureEquipItemDetails(ItemDetails itemDetails, IEquipItem equip)
        {
            itemDetails.Clear();
            itemDetails.SetItemIcon(equip.Icon!);
            itemDetails.SetItemName(equip.DisplayName);
            itemDetails.SetItemUpdateLevel(equip.UpdateLevel);
            foreach (var item in equip.BaseModifiers)
            {
                var selectable = new InteractiveLabel();
                selectable.SetSelectable(false);
                selectable.SetText(Lokalizator.Format(item));
                itemDetails.SetItemBaseStats(selectable);
            }

            foreach (var modifier in equip.AdditionalModifiers)
            {
                var stat = new InteractiveLabel();
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
    }
}

