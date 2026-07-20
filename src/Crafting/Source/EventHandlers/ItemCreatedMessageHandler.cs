namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Views.UI;
    using UIElements;

    /// <summary>Presentation only: shows the "item created" card. The item reaches the bag in
    /// CreateEquipItemRequestHandler — adding it here as well used to duplicate it.</summary>
    public class ItemCreatedMessageHandler(IUiElementsManager manager)
        : IMessageHandler<ItemCreatedMessage>
    {
        public Task HandleMessageAsync(ItemCreatedMessage message)
        {
            var notifier = ItemCreatedNotifier.Initialize().Instantiate<ItemCreatedNotifier>();
            ConfigureItemCreatedNotifier(message.CreatedItem, notifier);

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
