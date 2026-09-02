namespace Core.Data.Schema
{
    using GameData;
    using LootTable;
    using Tooling.Schema;

    /// <summary>
    /// A position naming the one thing it drops. One of the two shapes a seat at a loot table is
    /// written in; the game reads both into <see cref="TableRecord"/>, whose converter accepts either,
    /// so nothing is ever deserialized into this — it exists to say what the file looks like.
    /// </summary>
    public sealed record LootPositionById
    {
        /// <summary>The thing that drops, named out of every catalog a droppable thing is written in.</summary>
        [CatalogRef(DataCatalog.EquipItems), CatalogRef(DataCatalog.Items), CatalogRef(DataCatalog.Recipes), CatalogRef(DataCatalog.Resources)]
        public string Id { get; init; } = string.Empty;

        /// <summary>Loot units the seat costs, never gold.</summary>
        public float Price { get; init; }
    }

    /// <summary>
    /// A position naming a SET of augments by what its members are, of which one is drawn when the drop
    /// is minted. The other of the two shapes a seat is written in, and the reason the position is
    /// polymorphic at all: a set is one seat, so its share of the drop stays a designer's decision.
    /// </summary>
    public sealed record LootPositionByGroup
    {
        /// <summary>Which augments the seat may turn into. Its presence is what makes the position this
        /// shape rather than the other.</summary>
        public AugmentGroup? Augments { get; init; }

        /// <summary>Loot units the seat costs, never gold — one price for the whole set.</summary>
        public float Price { get; init; }
    }
}
