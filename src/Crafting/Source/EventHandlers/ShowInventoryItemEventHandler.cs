namespace Crafting.Source.EventHandlers
{
    using Utilities;
    using UIElements;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;

    public class ShowInventoryItemEventHandler(IInventory inventory, IUiElementProvider uiElementProvider)
        : IEventHandler<ShowInventoryItemEvent>
    {
        public Task HandleAsync(ShowInventoryItemEvent evnt)
        {
            var item = inventory.GetItem<IItem>(evnt.Item.InstanceId);
            if (item != null)
            {
                var details = uiElementProvider.CreateAndShowTooltip<ItemDetails>();
                ConfigureDetails(details, item);
            }
            return Task.CompletedTask;
        }

        private void ConfigureDetails(ItemDetails details, IItem item)
        {
            if (item is IEquipItem equip) ConfigureEquipItemDetails(details, equip);
            else ConfigureItemDetails(details, item);
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
                selectable.SetText(Localization.Format(item));
                itemDetails.SetItemBaseStats(selectable);
            }

            foreach (var modifier in equip.AdditionalModifiers)
            {
                var stat = new InteractiveLabel();
                stat.SetText(Localization.Format(modifier));
                stat.SetSelectable(false);
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
