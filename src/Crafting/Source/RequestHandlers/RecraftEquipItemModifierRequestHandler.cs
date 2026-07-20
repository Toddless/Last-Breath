namespace Crafting.Source.RequestHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Crafting;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.MessageBus.Requests;

    /// <summary>The handler is the price authority: the mandatory cost is recomputed HERE from the
    /// item (rarity/category base × the growing-reroll multiplier) — the request carries only the
    /// optional additives, so a stale UI price can never desync what is actually spent. Resources
    /// are spent only when the reroll actually happened — a non-rerollable instance id or an
    /// exhausted pool refuses for free.</summary>
    public class RecraftEquipItemModifierRequestHandler(
        IInventory inventory,
        IItemUpgrader itemUpgrader,
        CraftingResources resources,
        IGameMessageBus gameMessageBus)
        : IRequestHandler<RecraftEquipItemModifierRequest, RequestResult<string>>
    {
        public Task<RequestResult<string>> HandleRequest(RecraftEquipItemModifierRequest request)
        {
            var item = inventory.GetItem<IEquipItem>(request.ItemInstanceId);
            if (item == null)
                return Task.FromResult(new RequestResult<string>(false, "Item was not found", null));

            // Priced BEFORE the reroll: a success bumps RecraftCount, which must only affect the NEXT one.
            var cost = BuildCost(item, request.AdditiveResources);
            if (!resources.HasAll(cost))
                return Task.FromResult(new RequestResult<string>(false, "Not enough resources", null));

            var newInstanceId = itemUpgrader.TryRecraftModifier(item, request.ModifierInstanceId, request.AdditiveResources.Keys);
            if (newInstanceId == null)
                return Task.FromResult(new RequestResult<string>(false, "Modifier is not rerollable", null));

            resources.TrySpend(cost);
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(CraftingMode.Recraft, item.Rarity));
            return Task.FromResult(new RequestResult<string>(true, string.Empty, newInstanceId));
        }

        /// <summary>Computed mandatory price plus the request's additives, one spendable map
        /// (a resource present in both sums).</summary>
        private Dictionary<string, int> BuildCost(IEquipItem item, Dictionary<string, int> additives)
        {
            var cost = new Dictionary<string, int>(additives);
            foreach (var requirement in itemUpgrader.GetRecraftResourceCost(item).Where(entry => entry.Type == RequirementType.Resource))
                cost[requirement.Id] = cost.GetValueOrDefault(requirement.Id) + requirement.Amount;
            return cost;
        }
    }
}
