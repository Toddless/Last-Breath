namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Godot;

    public class DestroyItemMessageHandler(
        IInventory inventory,
        IItemDataProvider provider,
        CraftingMastery craftingMastery)
        : IMessageHandler<DestroyItemMessage>
    {
        private readonly ICraftingMastery _craftingMastery = craftingMastery;

        public Task HandleMessageAsync(DestroyItemMessage message)
        {
            var item = inventory.GetItem<IEquipItem>(message.ItemInstanceId);
            if (item == null) return Task.CompletedTask;
            // here for example we can later call a service with player´s mastery to get minimum amount resources to return
            foreach (var res in item.UsedResources)
            {
                int amount = Mathf.RoundToInt(res.Value * _craftingMastery.GetCurrentResourceMultiplier());
                if (inventory.TryAddItemStacks(res.Key, amount)) continue;

                var itemInstance = provider.CopyItem(res.Key);
                inventory.TryAddItem(itemInstance, amount);
            }

            // ______________________________________________________________________________________________________________
            inventory.RemoveItemByInstanceId(message.ItemInstanceId);

            return Task.CompletedTask;
        }
    }
}
