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
        public Task HandleMessageAsync(OpenCraftingWindowMessage message)
        {
            if (!message.IsItem || string.IsNullOrWhiteSpace(message.Id))
            {
                uiElementsManager.ToggleWindow(typeof(CraftingWindow));
                return Task.CompletedTask;
            }

            var window = (CraftingWindow)uiElementsManager.OpenWindow(typeof(CraftingWindow));
            var item = inventory.GetItem<IEquipItem>(message.Id);
            if (item != null && message.CraftingMode is CraftingMode.Upgrade or CraftingMode.Recraft or CraftingMode.Ascend)
                window.SetItem(item, message.CraftingMode);
            return Task.CompletedTask;
        }
    }
}
