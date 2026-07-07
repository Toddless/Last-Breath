namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;

    public class GetEquipItemUpgradeCostRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader)
        : IRequestHandler<GetEquipItemUpgradeCostRequest, IEnumerable<IRequirement>>
    {
        public Task<IEnumerable<IRequirement>> HandleRequest(GetEquipItemUpgradeCostRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null) return Task.FromResult<IEnumerable<IRequirement>>([]);

            var upgradeCosts = itemUpgrader.GetUpgradeResourceCost(item.Rarity, item.EquipmentPiece.ConvertEquipmentPartToCategory()) ;

            return Task.FromResult<IEnumerable<IRequirement>>(upgradeCosts);
        }
    }
}
