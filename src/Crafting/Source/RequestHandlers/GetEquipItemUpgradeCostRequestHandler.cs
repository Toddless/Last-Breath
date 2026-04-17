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
