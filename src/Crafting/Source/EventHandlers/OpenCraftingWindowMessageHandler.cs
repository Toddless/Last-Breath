namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.Views.UI;
    using UIElements;

    public class OpenCraftingWindowMessageHandler(
        IUiElementsManager uiElementsManager,
        IInventory inventory)
        : IMessageHandler<OpenCraftingWindowMessage>
    {
        public async Task HandleMessageAsync(OpenCraftingWindowMessage message)
        {
            if (!message.IsItem || string.IsNullOrWhiteSpace(message.Id))
            {
                uiElementsManager.OpenWindow(typeof(CraftingWindow));
                return;
            }

            var window = (CraftingWindow)uiElementsManager.OpenWindow(typeof(CraftingWindow));
            var item = inventory.GetItem<IEquipItem>(message.Id);
            if (item == null) return;
            switch (message.CraftingMode)
            {
                case CraftingMode.Recraft:
                    await window.SetEquipItemForRecraftAsync(item);
                    break;
                case CraftingMode.Upgrade:
                    await window.SetEquipItemForUpgradeAsync(item);
                    break;
            }
        }
    }
}
