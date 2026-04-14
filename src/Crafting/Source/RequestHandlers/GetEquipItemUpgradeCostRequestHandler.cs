namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Inventory;
    using Core.Interfaces.Items;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;

    public class GetEquipItemUpgradeCostRequestHandler : IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IRequirement>>
    {
        private readonly IInventory _inventory;
        private readonly IItemUpgrader _itemUpgrader;

        public GetEquipItemUpgradeCostRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader)
        {
            _itemUpgrader = itemUpgrader;
            _inventory = inventory;
        }

        public Task<IEnumerable<IRequirement>> HandleRequest(GetEquipItemUpgradeCostRequest request)
        {
            var item = _inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null) return Task.FromResult<IEnumerable<IRequirement>>([]);

            var upgradeCosts = _itemUpgrader?.GetUpgradeResourceCost(item.Rarity, item.EquipmentPiece.ConvertEquipmentPartToCategory(), request.Mode) ?? [];

            return Task.FromResult<IEnumerable<IRequirement>>(upgradeCosts);
        }
    }
}
