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

    public class
        RecraftEquipItemModifierRequestHandler(
            IInventory inventory,
            IItemUpgrader itemUpgrader,
            IGameMessageBus gameMessageBus)
        : IRequestHandler<RecraftEquipItemModifierRequest,
            RequestResult<IModifierInstance>>
    {
        public Task<RequestResult<IModifierInstance>> HandleRequest(RecraftEquipItemModifierRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null)
                return Task.FromResult(new RequestResult<IModifierInstance>(false, "Item was not fount", null));

            var modifiers = item.ModifiersPool.ToList();

            var mode = itemUpgrader.TryRecraftModifier(item, request.ModifierHash, modifiers);
            if (mode == null)
                return Task.FromResult(new RequestResult<IModifierInstance>(false, "Modifier is not rerollable", null));

            gameMessageBus.PublishMessageAsync(new ConsumeResourcesInInventoryMessage(request.Resources));
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(Core.Enums.CraftingMode.Recraft, item.Rarity));
            return Task.FromResult(new RequestResult<IModifierInstance>(true, string.Empty, mode));
        }
    }
}
