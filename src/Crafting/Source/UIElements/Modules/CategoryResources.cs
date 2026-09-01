namespace Crafting.Source.UIElements.Modules
{
    using System.Collections.Generic;
    using Core.Data;
    using Core.Inventory;

    /// <summary>Shared candidate lookup of a category requirement: members come from the resources'
    /// material data when the id names a real category; legacy bare-tag requirements fall back to
    /// the inventory tag lookup.</summary>
    public static class CategoryResources
    {
        /// <summary>Resource ids that can fill the category slot, before any owned/chosen filtering.</summary>
        public static IEnumerable<string> CandidateIds(IItemDataProvider? dataProvider, IInventory? inventory, string categoryId)
        {
            var members = dataProvider?.GetResourceIdsInCategory(categoryId) ?? [];
            return members.Count > 0 ? members : inventory?.GetAllItemIdsWithTag(categoryId) ?? [];
        }
    }
}
