namespace Crafting.Source.MediatorHandlers
{
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Events;
    using Core.Interfaces.Mediator.Requests;

    public class RecraftEquipItemModifierRequestHandler : IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<IModifierInstance>>
    {
        private readonly IInventory _inventory;
        private readonly IItemUpgrader _itemUpgrader;
        private readonly IItemDataProvider _itemDataProvider;
        private readonly ISystemMediator _systemMediator;

        public RecraftEquipItemModifierRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader, IItemDataProvider itemDataProvider, ISystemMediator systemMediator)
        {
            _itemUpgrader = itemUpgrader;
            _inventory = inventory;
            _itemDataProvider = itemDataProvider;
            _systemMediator = systemMediator;
        }

        public Task<RequestResult<IModifierInstance>> Handle(RecraftEquipItemModifierRequest request)
        {
            var item = _inventory.GetItem<IEquipItem>(request.ItemInstanceID);
            if (item == null) return Task.FromResult(new RequestResult<IModifierInstance>(false, "Item was not fount", null));
            var modifiers = new List<IMaterialModifier>();
            foreach (var resource in request.Resources)
                modifiers.AddRange(_itemDataProvider.GetResourceModifiers(resource.Key));

            var mode = _itemUpgrader.TryRecraftModifier(item, request.ModifierHash, modifiers);
            _systemMediator.Publish(new ConsumeResourcesInInventoryEvent(request.Resources));
            _systemMediator.Publish(new GainCraftingExpirienceEvent(Core.Enums.CraftingMode.Recraft, item.Rarity));
            return Task.FromResult(new RequestResult<IModifierInstance>(true, string.Empty, mode));
        }
    }
}
