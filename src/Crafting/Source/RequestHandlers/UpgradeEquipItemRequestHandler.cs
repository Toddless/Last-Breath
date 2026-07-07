namespace Crafting.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Results;

    public class UpgradeEquipItemRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader, IGameMessageBus gameMessageBus)
        : IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>
    {
        public Task<ItemUpgradeResult> HandleRequest(UpgradeEquipItemRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.InstanceId);
            if (item == null) return Task.FromResult(ItemUpgradeResult.Failure);
            gameMessageBus.PublishMessageAsync(new ConsumeResourcesInInventoryMessage(request.Resources));
            // Later, I need to pass the used resources to the TryUpgradeItem method (some of these resources influence the result).
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(Core.Enums.CraftingMode.Upgrade, item.Rarity));
            return Task.FromResult(itemUpgrader.TryUpgradeItem(item));
        }
    }
}
