namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Core.Interfaces.Mediator;
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
