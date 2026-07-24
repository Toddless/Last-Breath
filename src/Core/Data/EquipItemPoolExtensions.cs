namespace Core.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Items;
    using Modifiers;

    /// <summary>The single place candidate pools for equip item rolls are assembled (live-pool model:
    /// pools are re-read from the provider on every operation, so a json edit is immediately visible
    /// on items that already exist). Descriptors come out UNSCALED — callers apply their multiplier
    /// (loot difficulty, crafting quality, or the item's own <see cref="IEquipItem.PowerMultiplier"/>).</summary>
    public static class EquipItemPoolExtensions
    {
        /// <summary>Family pool (id prefix before the first underscore) ∪ the item's own pool. What loot
        /// generation rolls from and the base of every equip's live reroll pool.</summary>
        public static IEnumerable<IModifierDescriptor> GetGenerationPool(this IItemDataProvider data, string itemId) =>
            data.GetEquipItemModifierPool(itemId).Concat(data.GetEquipItemBaseModifierPool(itemId));

        /// <summary>The live reroll pool of an existing item: the generation union + the descriptors of
        /// every resource the item was crafted from — required AND optional parts alike (loot items
        /// have neither). Resource entries restricted to another equipment category are gated out.
        /// Per-operation additions (additive essences) are the caller's business.</summary>
        public static IEnumerable<IModifierDescriptor> GetLiveRerollPool(this IItemDataProvider data, IEquipItem item) =>
            data.GetGenerationPool(item.Id)
                .Concat(item.UsedRequiredResources.Keys
                    .Concat(item.UsedOptionalResources.Keys)
                    .Distinct()
                    .SelectMany(data.GetResourceDescriptors)
                    .ForCategory(item.EquipmentPiece.ConvertEquipmentPartToCategory()));

        /// <summary>Category gate for resource-fed pools: an entry from a byCategory section only
        /// serves its own equipment category; unrestricted entries serve all.</summary>
        public static IEnumerable<IModifierDescriptor> ForCategory(this IEnumerable<IModifierDescriptor> pool, EquipmentCategory category) =>
            pool.Where(descriptor => descriptor.OnlyFor is null || descriptor.OnlyFor == category);
    }
}
