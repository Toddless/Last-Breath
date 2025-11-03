namespace Crafting.Source.MediatorHandlers
{
    using Core.Results;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Mediator.Events;
    using Core.Interfaces.Mediator.Requests;

    public class UpgradeEquipItemRequestHandler : IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>
    {
        private readonly IInventory _inventory;
        private readonly IItemUpgrader _itemUpgrader;
        private readonly ISystemMediator _systemMediator;

        public UpgradeEquipItemRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader, ISystemMediator systemMediator)
        {
            _itemUpgrader = itemUpgrader;
            _inventory = inventory;
            _systemMediator = systemMediator;
        }

        public Task<ItemUpgradeResult> Handle(UpgradeEquipItemRequest request)
        {
            var item = _inventory.GetItem<IEquipItem>(request.InstanceId);
            if (item == null) return Task.FromResult(ItemUpgradeResult.Failure);
            _systemMediator.Publish(new ConsumeResourcesInInventoryEvent(request.Resources));
            // Later, I need to pass the used resources to the TryUpgradeItem method (some of these resources influence the result).
            _systemMediator.Publish(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Upgrade, item.Rarity));
            return Task.FromResult(_itemUpgrader.TryUpgradeItem(item));
        }
    }
}
