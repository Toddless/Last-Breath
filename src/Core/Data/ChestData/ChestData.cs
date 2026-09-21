namespace Core.Data.ChestData
{
    using System.Collections.Generic;
    using CraftingData;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;
    using World.Containers;

    /// <summary>The json names of the chest catalog, written once for the three that have to agree on
    /// them: the DTOs reading the file, the describer handing the shape to the authoring tool, and the
    /// reports naming the field at fault. A renamed field cannot leave any of them pointing at a field
    /// the file no longer has.</summary>
    public static class ChestFields
    {
        public const string Id = "id";

        public const string NameKey = "nameKey";

        public const string Delay = "emptyRemovalDelayMinutes";

        public const string Access = "access";

        public const string Contents = "contents";

        /// <summary>Written by the access and by the contents alike: one word, asked of two things.</summary>
        public const string Mode = "mode";

        public const string Items = "items";

        public const string SlotId = "slotId";

        public const string ItemId = "itemId";

        public const string Amount = "amount";

        public const string Rarity = "rarity";
    }

    /// <summary>
    /// What a chest file says, before any of it is believed. Every field is nullable, so a missing one
    /// reaches the reader as absent and the record is refused rather than defaulted; and every field is
    /// <c>required</c>, so the authoring tool lays each key down with a fresh record and will not take one
    /// away. What a chest IS once the record is believed is <see cref="ChestDefinition"/>, which carries
    /// no absent field at all.
    /// </summary>
    public sealed record ChestData
    {
        [JsonProperty(ChestFields.Id)] public required string? Id { get; init; }

        /// <summary>Key the chest is named under. Written out rather than derived from the id: what a
        /// container is called is the author's line, and two chests may stand under one name.</summary>
        [JsonProperty(ChestFields.NameKey)][LocalizedKey] public required string? NameKey { get; init; }

        /// <summary>Game minutes an emptied chest stays in the world before it is taken out of it.</summary>
        [JsonProperty(ChestFields.Delay)] public required double? EmptyRemovalDelayMinutes { get; init; }

        [JsonProperty(ChestFields.Access)] public required ChestAccessData? Access { get; init; }

        [JsonProperty(ChestFields.Contents)] public required ChestContentsData? Contents { get; init; }
    }

    /// <summary>How the chest is entered.</summary>
    public sealed record ChestAccessData
    {
        [JsonProperty(ChestFields.Mode)][EnumOf(typeof(ChestAccessMode))] public required string? Mode { get; init; }
    }

    /// <summary>Where the chest's positions come from, and the authored ones themselves.</summary>
    public sealed record ChestContentsData
    {
        [JsonProperty(ChestFields.Mode)][EnumOf(typeof(ChestContentsMode))] public required string? Mode { get; init; }

        [JsonProperty(ChestFields.Items)] public required List<ChestItemData?>? Items { get; init; }
    }

    /// <summary>One authored position: the slot it stands in, the thing minted into it and how many.</summary>
    public sealed record ChestItemData
    {
        /// <summary>Names the position inside its own chest and nothing outside it — the save writes what
        /// is left in the chest by this word, so it is a name the author picks rather than a reference.</summary>
        [JsonProperty(ChestFields.SlotId)][NotARef] public required string? SlotId { get; init; }

        /// <summary>Names one thing to mint, out of every catalog a thing placed in the world is written
        /// in: any one of them knowing the id makes the position real. In the resources that is the two
        /// sections holding things — a material category is what a resource belongs to, never a thing to
        /// pick up. The ornaments stand outside the list: one is handed out by a quest and by nothing
        /// else, so a chest may no more hold one than a loot table may price one.</summary>
        [JsonProperty(ChestFields.ItemId)]
        [CatalogRef(DataCatalog.EquipItems)]
        [CatalogRef(DataCatalog.Items)]
        [CatalogRef(DataCatalog.Recipes)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.UpgradeResourcesSection)]
        [CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        public required string? ItemId { get; init; }

        [JsonProperty(ChestFields.Amount)] public required int? Amount { get; init; }

        /// <summary>What the position is worth, which is also what it is minted at: a chest hands over
        /// the piece the author priced rather than a roll of its own.</summary>
        [JsonProperty(ChestFields.Rarity)][EnumOf(typeof(Rarity))] public required string? Rarity { get; init; }
    }
}
