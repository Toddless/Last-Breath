namespace Crafting.Source.MediatorHandlers
{
    using Core.Enums;
    using Core.Interfaces.Items;
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using System.Collections.Generic;
    using Core.Interfaces.Mediator.Requests;

    public class GetEquipItemUpgradeCostRequestHandler : IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IResourceRequirement>>
    {
        private readonly IInventory _inventory;
        private readonly IItemUpgrader _itemUpgrader;

        public GetEquipItemUpgradeCostRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader)
        {
            _itemUpgrader = itemUpgrader;
            _inventory = inventory;
        }

        public Task<IEnumerable<IResourceRequirement>> Handle(GetEquipItemUpgradeCostRequest request)
        {
            var item = _inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null) return Task.FromResult<IEnumerable<IResourceRequirement>>([]);

            var upgradeCosts = _itemUpgrader?.GetUpgradeResourceCost(item.Rarity, item.EquipmentPart.ConvertEquipmentPartToCategory(), request.Mode) ?? [];

            return Task.FromResult<IEnumerable<IResourceRequirement>>(upgradeCosts);
        }
    }
}
