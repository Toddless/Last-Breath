namespace Core.Data.LootTable
{
    using System;
    using System.Collections.Generic;
    using CraftingData;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Schema;

    /// <summary>
    /// One position of a loot table: what the budget buys when the position comes up, and what it
    /// costs in loot units. A position names ONE of two things and never both —
    /// <list type="bullet">
    /// <item>an <paramref name="Id"/>: this exact thing, the way a boss drops the one relic it is
    /// known for;</item>
    /// <item>an <see cref="AugmentGroup"/>: a set described by a filter, of which one member is drawn
    /// when the drop is minted.</item>
    /// </list>
    /// Either way the position is one seat at the table, so its share of the drop stays the
    /// designer's decision rather than a consequence of how many things answer it.
    /// </summary>
    /// <param name="Id">Names one thing to drop, out of every catalog a droppable thing is written in:
    /// any one of them knowing the id makes the position real. In the resources that is the two sections
    /// holding things — a material category is what a resource belongs to, never a drop of its own.</param>
    /// <param name="Price">Loot units, never gold. A group carries one price for the whole set: the
    /// price says what a thing of that kind is worth, and the members of a group are alike by
    /// construction — they share a tier and a rarity.</param>
    public record TableRecord(
        [property: CatalogRef(DataCatalog.EquipItems)]
        [property: CatalogRef(DataCatalog.Items)]
        [property: CatalogRef(DataCatalog.Recipes)]
        [property: CatalogRef(DataCatalog.Resources, Section = ResourcesData.UpgradeResourcesSection)]
        [property: CatalogRef(DataCatalog.Resources, Section = ResourcesData.CraftingResourcesSection)]
        string Id,
        float Price,
        AugmentGroup? Augments = null)
    {
        /// <summary>Whether the position names anything at all to drop. Positions also arrive from
        /// code — an NPC modifier adding to the table — where nothing has read them for sense, so
        /// every pick and every price walks past a position naming nothing.</summary>
        public bool NamesADrop => Augments != null || !string.IsNullOrWhiteSpace(Id);
    }

    /// <summary>
    /// Reads the positions of one tier. Per-entry tolerance, the way the rest of the catalogs are
    /// read: a position the file gets wrong is reported and dropped alone, because a typo in one
    /// line must not cost a kill its whole table — and never silently defaulted, because a position
    /// defaulted into existence would burn budget on nothing at every kill for the rest of the game.
    /// </summary>
    public sealed class TableRecordsConverter : JsonConverter<List<TableRecord>>
    {
        private const string IdProperty = "id";
        private const string PriceProperty = "price";
        private const string GroupProperty = "augments";
        private const string TierProperty = "tier";
        private const string RarityProperty = "rarity";

        public override List<TableRecord> ReadJson(
            JsonReader reader,
            Type objectType,
            List<TableRecord>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            List<TableRecord> records = [];
            if (JToken.Load(reader) is not JArray entries)
            {
                Tracker.TrackError("Skipping the positions of a loot table tier: they must be written as a list.");
                return records;
            }

            foreach (JToken entry in entries)
                if (ReadRecord(entry) is { } record)
                    records.Add(record);

            return records;
        }

        public override void WriteJson(JsonWriter writer, List<TableRecord>? value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (TableRecord record in value ?? [])
            {
                writer.WriteStartObject();
                if (record.Augments is { } group) WriteGroup(writer, group);
                else WriteProperty(writer, IdProperty, record.Id);
                WriteProperty(writer, PriceProperty, record.Price);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteGroup(JsonWriter writer, AugmentGroup group)
        {
            writer.WritePropertyName(GroupProperty);
            writer.WriteStartObject();
            WriteProperty(writer, TierProperty, group.Tier);
            WriteProperty(writer, RarityProperty, group.Rarity.ToString());
            writer.WriteEndObject();
        }

        private static void WriteProperty(JsonWriter writer, string name, object value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

        private static TableRecord? ReadRecord(JToken entry)
        {
            if (entry is not JObject position)
            {
                Tracker.TrackError($"Skipping a loot table position: expected a record, got {entry.Type}.");
                return null;
            }

            string id = (string?)position[IdProperty] ?? string.Empty;
            JToken? group = position[GroupProperty];
            bool namesId = !string.IsNullOrWhiteSpace(id);
            bool namesGroup = group is { Type: not JTokenType.Null };

            // Both answers to one question, or neither: the first leaves what drops undecided, the
            // second describes nothing at all. Guessing either way puts a price on a mystery.
            if (namesId == namesGroup)
            {
                Tracker.TrackError($"Skipping a loot table position: it must name exactly one of '{IdProperty}' and '{GroupProperty}' ({Describe(position)}).");
                return null;
            }

            if (ReadPrice(position) is not { } price) return null;
            if (!namesGroup) return new TableRecord(id, price);

            return ReadGroup(group!, position) is { } augments ? new TableRecord(string.Empty, price, augments) : null;
        }

        // A free position is bought over and over for nothing: the budget is what makes a table a
        // table, so a position that does not take from it is refused rather than read as "free".
        private static float? ReadPrice(JObject position)
        {
            JToken? price = position[PriceProperty];
            if (price is { Type: JTokenType.Integer or JTokenType.Float } && (float)price > 0f) return (float)price;

            Tracker.TrackError($"Skipping a loot table position: '{PriceProperty}' must be a positive number of loot units ({Describe(position)}).");
            return null;
        }

        private static AugmentGroup? ReadGroup(JToken group, JObject position)
        {
            if (group is not JObject filter)
            {
                Tracker.TrackError($"Skipping a loot table position: '{GroupProperty}' must be a filter record ({Describe(position)}).");
                return null;
            }

            JToken? tier = filter[TierProperty];
            if (tier is not { Type: JTokenType.Integer } || (int)tier < 0)
            {
                Tracker.TrackError($"Skipping a loot table position: '{GroupProperty}.{TierProperty}' must be a tier number ({Describe(position)}).");
                return null;
            }

            string rarity = (string?)filter[RarityProperty] ?? string.Empty;
            if (!EnumParser.TryParseEnum(rarity, out Rarity parsed))
            {
                Tracker.TrackError($"Skipping a loot table position: '{rarity}' is not a {nameof(Rarity)} ({Describe(position)}).");
                return null;
            }

            return new AugmentGroup((int)tier, parsed);
        }

        private static string Describe(JObject position) => position.ToString(Formatting.None);
    }
}
