namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Interfaces;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;

    public class GetEquipItemRecraftModifierCostRequestHandler : IRequestHandler<GetEquipItemRecraftModifierCostRequest, IEnumerable<IRequirement>>
    {
        private readonly IInventory _inventory;
        private readonly IItemUpgrader _itemUpgrader;

        public GetEquipItemRecraftModifierCostRequestHandler(IInventory inventory, IItemUpgrader itemUpgrader)
        {
            _itemUpgrader = itemUpgrader;
            _inventory = inventory;
        }

        public Task<IEnumerable<IRequirement>> HandleRequest(GetEquipItemRecraftModifierCostRequest request)
        {
            var item = _inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null) return Task.FromResult<IEnumerable<IRequirement>>([]);

            // The item-aware price (includes the growing-reroll multiplier) — the same one the recraft handler spends.
            var recraftCost = _itemUpgrader?.GetRecraftResourceCost(item) ?? [];

            return Task.FromResult<IEnumerable<IRequirement>>(recraftCost);
        }
    }
}
