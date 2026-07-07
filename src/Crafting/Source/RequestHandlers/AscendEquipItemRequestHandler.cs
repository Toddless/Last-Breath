namespace Crafting.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Results;

    public class AscendEquipItemRequestHandler(IInventory inventory, IItemAscender itemAscender, IGameMessageBus gameMessageBus)
        : IRequestHandler<AscendEquipItemRequest, AscensionResult>
    {
        public Task<AscensionResult> HandleRequest(AscendEquipItemRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.InstanceId);
            if (item == null) return Task.FromResult(new AscensionResult(false, null));

            var result = itemAscender.TryAscendItem(item);
            if (result.Succeeded)
                gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(CraftingMode.Ascend, item.Rarity));

            return Task.FromResult(result);
        }
    }
}
