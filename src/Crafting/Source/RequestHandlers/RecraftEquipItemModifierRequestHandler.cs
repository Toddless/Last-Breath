namespace Crafting.Source.RequestHandlers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Modifiers;

    /// <summary>Resources are spent only when the reroll actually happened — a non-rerollable
    /// hash or an exhausted pool refuses for free.</summary>
    public class RecraftEquipItemModifierRequestHandler(
        IInventory inventory,
        IItemUpgrader itemUpgrader,
        CraftingResources resources,
        IGameMessageBus gameMessageBus)
        : IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>
    {
        public Task<RequestResult<IModifierInstance>> HandleRequest(RecraftEquipItemModifierRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null)
                return Task.FromResult(new RequestResult<IModifierInstance>(false, "Item was not found", null));
            if (!resources.HasAll(request.Resources))
                return Task.FromResult(new RequestResult<IModifierInstance>(false, "Not enough resources", null));

            var modifiers = item.ModifiersPool.ToList();
            var mode = itemUpgrader.TryRecraftModifier(item, request.ModifierHash, modifiers, request.Resources.Keys);
            if (mode == null)
                return Task.FromResult(new RequestResult<IModifierInstance>(false, "Modifier is not rerollable", null));

            resources.TrySpend(request.Resources);
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(Core.Enums.CraftingMode.Recraft, item.Rarity));
            return Task.FromResult(new RequestResult<IModifierInstance>(true, string.Empty, mode));
        }
    }
}
