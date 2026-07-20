namespace Crafting.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
    using Core.Results;

    /// <summary>Order matters: cap check → spend (all or nothing) → roll → exp. A failed roll
    /// still pays and still teaches — the attempt is the lesson; a capped item costs nothing.</summary>
    public class UpgradeEquipItemRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader, CraftingResources resources, IGameMessageBus gameMessageBus)
        : IRequestHandler<UpgradeEquipItemRequest, ItemUpgradeResult>
    {
        public Task<ItemUpgradeResult> HandleRequest(UpgradeEquipItemRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.InstanceId);
            if (item == null) return Task.FromResult(ItemUpgradeResult.Failure);
            if (item.UpdateLevel == item.MaxUpdateLevel) return Task.FromResult(ItemUpgradeResult.ReachedMaxLevel);
            if (!resources.TrySpend(request.Resources)) return Task.FromResult(ItemUpgradeResult.NotEnoughResources);

            var result = itemUpgrader.TryUpgradeItem(item, request.Resources.Keys);
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(Core.Enums.CraftingMode.Upgrade, item.Rarity));
            return Task.FromResult(result);
        }
    }
}
