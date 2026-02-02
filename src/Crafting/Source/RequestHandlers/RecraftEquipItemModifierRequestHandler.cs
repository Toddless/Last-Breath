namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Data;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Events;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Requests;
    using Core.Modifiers;

    public class
        RecraftEquipItemModifierRequestHandler(
            IInventory inventory,
            IItemUpgrader itemUpgrader,
            IItemDataProvider itemDataProvider,
            IMediator mediator)
        : IRequestHandler<RecraftEquipItemModifierRequest,
            RequestResult<IModifierInstance>>
    {
        public Task<RequestResult<IModifierInstance>> Handle(RecraftEquipItemModifierRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.ItemInstanceID);
            if (item == null)
                return Task.FromResult(new RequestResult<IModifierInstance>(false, "Item was not fount", null));
            var modifiers = new List<IModifier>();
            foreach (var resource in request.Resources)
                modifiers.AddRange(itemDataProvider.GetResourceModifiers(resource.Key));

            var mode = itemUpgrader.TryRecraftModifier(item, request.ModifierHash, modifiers);
            mediator.PublishAsync(new ConsumeResourcesInInventoryEvent(request.Resources));
            mediator.PublishAsync(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Recraft, item.Rarity));
            return Task.FromResult(new RequestResult<IModifierInstance>(true, string.Empty, mode));
        }
    }
}
