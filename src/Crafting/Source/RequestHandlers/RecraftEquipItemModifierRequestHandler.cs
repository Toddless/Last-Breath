namespace Crafting.Source.RequestHandlers
{
    using System.Linq;
    using Core.Modifiers;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Events;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;

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
            gameMessageBus.PublishMessageAsync(new ConsumeResourcesInInventoryMessage(request.Resources));
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(Core.Enums.CraftingMode.Recraft, item.Rarity));
            return Task.FromResult(new RequestResult<IModifierInstance>(true, string.Empty, mode));
        }
    }
}
