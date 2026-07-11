namespace Crafting.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Inventory;

    /// <summary>
    /// Check-then-spend for crafting costs: nothing is consumed unless EVERYTHING is present.
    /// The single gate every crafting command goes through — the UI merely mirrors HasAll.
    /// </summary>
    public class CraftingResources(IInventory inventory)
    {
        public bool HasAll(IReadOnlyDictionary<string, int> resources) =>
            resources.All(pair => inventory.GetTotalItemAmount(pair.Key) >= pair.Value);

        public bool TrySpend(IReadOnlyDictionary<string, int> resources)
        {
            if (!HasAll(resources)) return false;

            foreach ((string id, int amount) in resources)
                inventory.RemoveItemById(id, amount);
            return true;
        }
    }
}
