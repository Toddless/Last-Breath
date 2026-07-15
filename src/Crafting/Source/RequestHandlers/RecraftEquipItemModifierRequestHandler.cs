namespace Crafting.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;

    /// <summary>Resources are spent only when the reroll actually happened — a non-rerollable
    /// instance id or an exhausted pool refuses for free.</summary>
    public class RecraftEquipItemModifierRequestHandler(
        IInventory inventory,
        IItemUpgrader itemUpgrader,
        CraftingResources resources,
        IGameMessageBus gameMessageBus)
        : IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<string>>
    {
        public Task<RequestResult<string>> HandleRequest(RecraftEquipItemModifierRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null)
                return Task.FromResult(new RequestResult<string>(false, "Item was not found", null));
            if (!resources.HasAll(request.Resources))
                return Task.FromResult(new RequestResult<string>(false, "Not enough resources", null));

            var newInstanceId = itemUpgrader.TryRecraftModifier(item, request.ModifierInstanceId, request.Resources.Keys);
            if (newInstanceId == null)
                return Task.FromResult(new RequestResult<string>(false, "Modifier is not rerollable", null));

            resources.TrySpend(request.Resources);
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(Core.Enums.CraftingMode.Recraft, item.Rarity));
            return Task.FromResult(new RequestResult<string>(true, string.Empty, newInstanceId));
        }
    }
}
