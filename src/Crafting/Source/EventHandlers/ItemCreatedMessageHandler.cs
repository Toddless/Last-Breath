namespace Crafting.Source.EventHandlers
{
    using UIElements;
    using Utilities;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.UI;

    public class ItemCreatedMessageHandler(IUiElementsManager manager, IInventory inventory)
        : IMessageHandler<ItemCreatedMessage>
    {
        public Task HandleMessageAsync(ItemCreatedMessage message)
        {
            var notifier =ItemCreatedNotifier.Initialize().Instantiate<ItemCreatedNotifier>();
            ConfigureItemCreatedNotifier(message.CreatedItem, notifier);

            inventory.TryAddItem(message.CreatedItem);

            notifier.DestroyPressed += OnDestroyPressed;

            void OnDestroyPressed()
            {
                // _systemMediator.Publish(new DestroyItemEvent(evnt.CreatedItem.InstanceId));
                notifier.DestroyPressed -= OnDestroyPressed;
            }

            return Task.CompletedTask;
        }

        private ItemCreatedNotifier ConfigureItemCreatedNotifier(IItem item, ItemCreatedNotifier instance)
        {
            instance.SetItemDetails(CreateItemDetails(item));
            return instance;
        }

        private ItemDetails CreateItemDetails(IItem item)
        {
            var itemDetails =ItemDetails.Initialize().Instantiate<ItemDetails>();

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
            foreach (var item in equip.Implicits)
            {
                var selectable = new InteractiveLabel();
                selectable.SetText(Localization.Format(item));
                itemDetails.SetItemBaseStats(selectable);
            }

            foreach (var modifier in equip.Modifiers)
            {
                var stat = new InteractiveLabel();
                stat.SetText(Localization.Format(modifier));
                itemDetails.SetItemAdditionalStats(stat);
            }

            // if (equip.ItemEffect != null)
            // {
            //     var skill = equip.ItemEffect;
            //     var skillDescription = SkillDescription.Initialize().Instantiate<SkillDescription>();
            //     skillDescription.SetSkillName(skill.DisplayName);
            //     skillDescription.SetSkillDescription(skill.Description);
            //     itemDetails.SetSkillDescription(skillDescription);
            // }

            return itemDetails;
        }
    }
}
