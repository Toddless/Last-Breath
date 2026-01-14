namespace Crafting.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Requests;
    using Core.Results;

    public class UpgradeEquipItemRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader, IMediator mediator)
        : IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>
    {
        public Task<ItemUpgradeResult> Handle(UpgradeEquipItemRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.InstanceId);
            if (item == null) return Task.FromResult(ItemUpgradeResult.Failure);
            mediator.PublishAsync(new ConsumeResourcesInInventoryEvent(request.Resources));
            // Later, I need to pass the used resources to the TryUpgradeItem method (some of these resources influence the result).
            mediator.PublishAsync(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Upgrade, item.Rarity));
            return Task.FromResult(itemUpgrader.TryUpgradeItem(item));
        }
    }
}
