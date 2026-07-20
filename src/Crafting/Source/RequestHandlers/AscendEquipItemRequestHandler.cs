namespace Crafting.Source.RequestHandlers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;
    using Core.Results;

    /// <summary>Ascension has a price now: the UpgradeCosts "ascend" section per category,
    /// spent all-or-nothing before the item transforms.</summary>
    public class AscendEquipItemRequestHandler(IInventory inventory, IItemAscender itemAscender, CraftingResources resources, IGameMessageBus gameMessageBus)
        : IRequestHandler<AscendEquipItemRequest, AscensionResult>
    {
        public Task<AscensionResult> HandleRequest(AscendEquipItemRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.InstanceId);
            if (item == null || !itemAscender.CanAscend(item)) return Task.FromResult(new AscensionResult(false, []));

            var cost = itemAscender.GetAscendResourceCost(item.EquipmentPiece.ConvertEquipmentPartToCategory())
                .Where(requirement => requirement.Type == RequirementType.Resource)
                .ToDictionary(requirement => requirement.Id, requirement => requirement.Amount);
            if (!resources.TrySpend(cost)) return Task.FromResult(new AscensionResult(false, []));

            var result = itemAscender.TryAscendItem(item);
            if (result.Succeeded)
                gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(CraftingMode.Ascend, item.Rarity));

            return Task.FromResult(result);
        }
    }
}
