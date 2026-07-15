namespace Crafting.Source.EventHandlers
{
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Godot;

    /// <summary>Shattering a crafted item: part of the used resources comes back (mastery scales
    /// the refund) and the attempt teaches — the Shatter exp factor finally has a consumer.</summary>
    public class DestroyItemMessageHandler(
        IInventory inventory,
        IItemDataProvider provider,
        ICraftingMastery craftingMastery,
        IGameMessageBus gameMessageBus)
        : IMessageHandler<DestroyItemMessage>
    {
        public Task HandleMessageAsync(DestroyItemMessage message)
        {
            var item = inventory.GetItem<IEquipItem>(message.ItemInstanceId);
            if (item == null) return Task.CompletedTask;

            foreach (var res in item.UsedResources)
            {
                int amount = Mathf.RoundToInt(res.Value * craftingMastery.GetCurrentResourceMultiplier());
                if (inventory.TryAddItemStacks(res.Key, amount)) continue;

                var itemInstance = provider.CopyItem(res.Key);
                inventory.TryAddItem(itemInstance, amount);
            }

            inventory.RemoveItemByInstanceId(message.ItemInstanceId);
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(CraftingMode.Shatter, item.Rarity));

            return Task.CompletedTask;
        }
    }
}
