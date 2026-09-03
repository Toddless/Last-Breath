namespace Core.Narrative
{
    using System.Collections.Generic;
    using Data.CraftingData;
    using Data.GameData;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The one reading of "an item, by id": the key it is written under, the amount beside it, and
    /// everywhere an item may be written. Giving, taking, asking after and rewarding an item all point
    /// here, so the four cannot drift apart over the same parameter.
    /// </summary>
    /// <remarks>The targets are said twice — as constants an attribute can take and as the specification a
    /// factory declares — because data markup accepts nothing but compile-time constants. Both forms stand
    /// in this one file, and a test holds them to each other.</remarks>
    public static class ItemReference
    {
        public const string Key = "itemId";

        public const string AmountKey = "amount";

        public const int DefaultAmount = 1;

        /// <summary>Equipment: minted fresh, so a reward is a roll of its own rather than a copy.</summary>
        public const string EquipItems = DataCatalog.EquipItems;

        /// <summary>Plain items, copied from their template.</summary>
        public const string Items = DataCatalog.Items;

        /// <summary>A recipe is a thing in the bag as much as a thing to craft with: it is copied by id like
        /// any item, drops from loot tables and is learned from the copy the player holds.</summary>
        public const string Recipes = DataCatalog.Recipes;

        /// <summary>Ornaments come through quests alone — no loot seat can buy one — which is the one way
        /// the item vocabulary is wider than the loot tables'.</summary>
        public const string Ornaments = DataCatalog.Ornaments;

        public const string Resources = DataCatalog.Resources;

        /// <summary>The two sections of the resources holding things. The third holds the categories a
        /// material belongs to, which is never a thing anyone hands over.</summary>
        public const string UpgradeResources = ResourcesData.UpgradeResourcesSection;

        public const string CraftingResources = ResourcesData.CraftingResourcesSection;

        private static readonly NarrativeReferenceTarget[] s_targets =
        [
            NarrativeReferenceTarget.Whole(EquipItems),
            NarrativeReferenceTarget.Whole(Items),
            NarrativeReferenceTarget.Whole(Recipes),
            NarrativeReferenceTarget.Whole(Ornaments),
            new(Resources, UpgradeResources),
            new(Resources, CraftingResources)
        ];

        /// <summary>The id itself, as a factory declares it.</summary>
        public static readonly NarrativeParameterSpec Field =
            NarrativeParameterSchema.Reference(Key, required: true, s_targets);

        /// <summary>How many of it, written beside the id or left to the one the parsers read.</summary>
        public static readonly NarrativeParameterSpec AmountField =
            NarrativeParameterSchema.Integer(AmountKey, DefaultAmount);

        /// <summary>Everywhere an item id may be written; any one of the targets knowing it makes the
        /// reference real. The minter is asked for the raw id either way, so an item minted outside every
        /// catalog — gold, an augment — is named just as well: the targets are what an editor offers, not
        /// a gate the game closes.</summary>
        public static IReadOnlyList<NarrativeReferenceTarget> Targets => s_targets;

        public static int Amount(JObject json) => json.Value<int?>(AmountKey) ?? DefaultAmount;

        public static string? Require(JObject json, string type)
        {
            string itemId = json.Value<string>(Key) ?? string.Empty;
            if (itemId.Length > 0) return itemId;

            Tracker.TrackError($"{type} action: {Key} is missing");
            return null;
        }
    }
}
