namespace Crafting.Source.MediatorHandlers
{
    using Core.Enums;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Requests;

    public class GetEquipItemRecraftModifierCostRequestHandler : IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IResourceRequirement>>
    {
        private readonly IInventory _inventory;
        private readonly IItemUpgrader _itemUpgrader;

        public GetEquipItemRecraftModifierCostRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader)
        {
            _itemUpgrader = itemUpgrader;
            _inventory = inventory;
        }

        public Task<IEnumerable<IResourceRequirement>> Handle(GetEquipItemRecraftModifierCostRequest request)
        {
            var item = _inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null) return Task.FromResult<IEnumerable<IResourceRequirement>>([]);

            var recraftCost = _itemUpgrader?.GetRecraftResourceCost(item.Rarity, item.EquipmentPart.ConvertEquipmentPartToCategory()) ?? [];

            return Task.FromResult<IEnumerable<IResourceRequirement>>(recraftCost);
        }
    }
}
