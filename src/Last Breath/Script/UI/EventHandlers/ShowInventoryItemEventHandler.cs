namespace Crafting.Source.MediatorHandlers.EventHandlers
{
    using Utilities;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using Crafting.Source.UIElements;
    using Core.Interfaces.Mediator.Events;

    public class ShowInventoryItemEventHandler : IEventHandler<ShowInventoryItemEvent>
    {
        private readonly IInventory _inventory;
        private readonly IUIElementProvider _uiElementProvider;

        public ShowInventoryItemEventHandler(IInventory inventory, IUIElementProvider uiElementProvider)
        {
            // TODO: Should i remove this from crafting dll??
            _inventory = inventory;
            _uiElementProvider = uiElementProvider;
        }

        public void Handle(ShowInventoryItemEvent evnt)
        {
            var item = _inventory.GetItem<IItem>(evnt.Item.InstanceId);
            if (item != null)
            {
                var details = _uiElementProvider.CreateAndShowTooltip<ItemDetails>();
                ConfigureDetails(details, item);
            }
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
                selectable.SetText(Localizator.Format(item));
                itemDetails.SetItemBaseStats(selectable);
            }

            foreach (var modifier in equip.AdditionalModifiers)
            {
                var stat = new InteractiveLabel();
                stat.SetText(Localizator.Format(modifier));
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
