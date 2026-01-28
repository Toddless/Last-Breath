namespace Crafting.Source.EventHandlers
{
    using Godot;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;

    public class DestroyItemEventHandler(
        IInventory inventory,
        IItemDataProvider provider,
        CraftingMastery craftingMastery)
        : IEventHandler<DestroyItemEvent>
    {
        private readonly ICraftingMastery _craftingMastery = craftingMastery;

        public Task HandleAsync(DestroyItemEvent request)
        {
            var item = inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null) return Task.CompletedTask;
            // here for example we can later call a service with player´s mastery to get minimum amount resources to return
            foreach (var res in item.UsedResources)
            {
                int amount = Mathf.RoundToInt(res.Value * _craftingMastery.GetResourceMultiplier());
                if (inventory.TryAddItemStacks(res.Key, amount)) continue;

                var itemInstance = provider.CopyBaseItem(res.Key);
                inventory.TryAddItem(itemInstance, amount);
            }

            // ______________________________________________________________________________________________________________
            inventory.RemoveItemByInstanceId(request.ItemInstanceId);
            // _systemMediator.Publish(new GainCraftingExperienceEvent(Core.Enums.CraftingMode.Shatter, item.Rarity));

            return Task.CompletedTask;
        }
    }
}
