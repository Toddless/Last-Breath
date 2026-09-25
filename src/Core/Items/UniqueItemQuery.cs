namespace Core.Items
{
    using Battle.Abilities;
    using Data;
    using Enums;

    /// <summary>
    /// Whether an item id names a one-of-a-kind thing. Asked by whoever is about to hand an item over,
    /// which is why it answers from the catalogs and mints nothing: the question comes BEFORE the item
    /// exists, and minting one to read its rarity would already be the duplicate.
    /// </summary>
    public interface IUniqueItemQuery
    {
        /// <summary>False for an id no catalog claims — an ordinary stack is the common case, and the
        /// mint reports an id that names nothing at all when it fails to build it.</summary>
        bool IsUnique(string itemId);
    }

    /// <inheritdoc cref="IUniqueItemQuery"/>
    /// <remarks>Two catalogs carry a template rarity generation never rerolls: an equip blueprint
    /// stamped Unique or Mythic, and the ornament catalog, whose every record is one of the handful of
    /// quest artefacts. Ids outside them draw their rarity elsewhere — an augment is worth what the seat
    /// dropping it says, a resource is a stack — so they are not one of a kind.</remarks>
    public sealed class UniqueItemQuery(IEquipBlueprintProvider blueprints, IOrnamentCatalog ornaments) : IUniqueItemQuery
    {
        public bool IsUnique(string itemId) =>
            ornaments.Find(itemId) != null || IsFixedTemplateRarity(blueprints.GetBlueprint(itemId)?.Rarity);

        /// <summary>The two rarities a template keeps: generation rolls every other one, so a blueprint
        /// carrying one of these IS the item rather than a draw of it.</summary>
        private static bool IsFixedTemplateRarity(Rarity? rarity) => rarity is Rarity.Unique or Rarity.Mythic;
    }
}
