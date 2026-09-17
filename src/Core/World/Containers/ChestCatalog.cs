namespace Core.World.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Data.GameData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>How a chest is entered. Locked modes wait on the lock design.</summary>
    public enum ChestAccessMode
    {
        Unlocked
    }

    /// <summary>Where a chest's positions come from. Generated modes wait on the loot-container design.</summary>
    public enum ChestContentsMode
    {
        Authored
    }

    /// <summary>One authored position: a stable slot, the item minted into it and how many.</summary>
    public record AuthoredChestItem(string SlotId, string ItemId, int Amount, Rarity Rarity);

    public record ChestAccess(ChestAccessMode Mode);

    public record ChestContentsDefinition(ChestContentsMode Mode, IReadOnlyList<AuthoredChestItem> Items);

    public record ChestDefinition(
        string Id,
        string NameKey,
        double EmptyRemovalDelayMinutes,
        ChestAccess Access,
        ChestContentsDefinition Contents);

    /// <summary>
    /// The chest catalog. Json records are read into DTOs and converted one by one: a record that fails
    /// a rule is reported with its id and the field at fault and skipped, so its neighbours in the file
    /// still load. Only a file that cannot be read at all takes the whole file down with it.
    /// </summary>
    public sealed class ChestCatalog : IGameDataParticipant
    {
        /// <summary>Stands in for the chest id in a report about the id itself.</summary>
        private const string UnnamedChest = "<no id>";

        private readonly Dictionary<string, ChestDefinition> _definitions = [];

        public IReadOnlyList<string> Catalogs => [DataCatalog.Chests];

        public ChestDefinition Get(string id) => _definitions.TryGetValue(id, out var definition)
            ? definition : throw new InvalidOperationException($"Unknown chest definition '{id}'.");

        public void Apply(string catalog, GameDataFile file)
        {
            var records = JsonConvert.DeserializeObject<List<ChestData?>>(file.Json)
                ?? throw new InvalidOperationException($"Failed to deserialize chests file '{file.FileName}'.");

            foreach (var record in records)
            {
                if (Read(record, file.FileName) is not { } definition) continue;
                if (!_definitions.TryAdd(definition.Id, definition))
                    Report(definition.Id, file.FileName, Field.Id, "is already taken by another chest");
            }
        }

        private static ChestDefinition? Read(ChestData? record, string file)
        {
            if (ReadText(record?.Id, UnnamedChest, file, Field.Id) is not { } id) return null;
            if (ReadText(record?.NameKey, id, file, Field.NameKey) is not { } nameKey) return null;
            if (record?.EmptyRemovalDelayMinutes is not { } delay || !double.IsFinite(delay) || delay < 0)
            {
                Report(id, file, Field.Delay, "must be a finite, non-negative number of game minutes");
                return null;
            }

            if (ReadAccess(record.Access, id, file) is not { } access) return null;
            if (ReadContents(record.Contents, id, file) is not { } contents) return null;

            return new ChestDefinition(id, nameKey, delay, access, contents);
        }

        private static ChestAccess? ReadAccess(ChestAccessData? data, string id, string file) =>
            ReadMode<ChestAccessMode>(data?.Mode, id, file, Field.AccessMode) is { } mode ? new ChestAccess(mode) : null;

        private static ChestContentsDefinition? ReadContents(ChestContentsData? data, string id, string file)
        {
            if (ReadMode<ChestContentsMode>(data?.Mode, id, file, Field.ContentsMode) is not { } mode) return null;
            if (data?.Items is not { Count: > 0 } entries)
            {
                Report(id, file, Field.ContentsItems, "lists no positions");
                return null;
            }

            List<AuthoredChestItem> items = [];
            foreach (var entry in entries)
            {
                if (ReadItem(entry, id, file) is not { } item) return null;
                items.Add(item);
            }

            if (items.Select(item => item.SlotId).Distinct().Count() == items.Count)
                return new ChestContentsDefinition(mode, items);

            Report(id, file, Field.ItemSlotId, "is repeated: every position needs its own slot");
            return null;
        }

        private static AuthoredChestItem? ReadItem(ChestItemData? entry, string id, string file)
        {
            if (ReadText(entry?.SlotId, id, file, Field.ItemSlotId) is not { } slotId) return null;
            if (ReadText(entry?.ItemId, id, file, Field.ItemItemId) is not { } itemId) return null;
            if (entry?.Amount is not { } amount || amount <= 0)
            {
                Report(id, file, Field.ItemAmount, "must be a positive count");
                return null;
            }

            return ReadMode<Rarity>(entry.Rarity, id, file, Field.ItemRarity) is { } rarity
                ? new AuthoredChestItem(slotId, itemId, amount, rarity) : null;
        }

        private static string? ReadText(string? value, string id, string file, string field)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value;

            Report(id, file, field, "is missing");
            return null;
        }

        /// <summary>A named member and nothing else — <see cref="EnumParser"/> refuses the comma lists and
        /// bare numbers that would otherwise mint a mode no author ever wrote.</summary>
        private static TEnum? ReadMode<TEnum>(string? value, string id, string file, string field)
            where TEnum : struct, Enum
        {
            if (!string.IsNullOrWhiteSpace(value) && EnumParser.TryParseEnum<TEnum>(value, out var mode)) return mode;

            Report(id, file, field, string.IsNullOrWhiteSpace(value)
                ? "is missing" : $"names no {typeof(TEnum).Name}: '{value}'");
            return null;
        }

        private static void Report(string id, string file, string field, string reason) =>
            Tracker.TrackError($"Skipping chest '{id}' from '{file}': '{field}' {reason}.");

        /// <summary>The json field names, shared by the DTOs that read them and the reports that name
        /// them, so a renamed field cannot leave a report pointing at a field the file no longer has.</summary>
        private static class Field
        {
            public const string Id = "id";
            public const string NameKey = "nameKey";
            public const string Delay = "emptyRemovalDelayMinutes";
            public const string Access = "access";
            public const string Contents = "contents";
            public const string Mode = "mode";
            public const string Items = "items";
            public const string SlotId = "slotId";
            public const string ItemId = "itemId";
            public const string Amount = "amount";
            public const string Rarity = "rarity";

            public const string AccessMode = Access + Separator + Mode;
            public const string ContentsMode = Contents + Separator + Mode;
            public const string ContentsItems = Contents + Separator + Items;
            public const string ItemSlotId = ContentsItems + Separator + SlotId;
            public const string ItemItemId = ContentsItems + Separator + ItemId;
            public const string ItemAmount = ContentsItems + Separator + Amount;
            public const string ItemRarity = ContentsItems + Separator + Rarity;

            private const string Separator = ".";
        }

        /// <summary>What the file says, before any of it is believed: every field is optional here so a
        /// missing one is a rejected record rather than a silent default.</summary>
        private sealed record ChestData
        {
            [JsonProperty(Field.Id)] public string? Id { get; init; }
            [JsonProperty(Field.NameKey)] public string? NameKey { get; init; }
            [JsonProperty(Field.Delay)] public double? EmptyRemovalDelayMinutes { get; init; }
            [JsonProperty(Field.Access)] public ChestAccessData? Access { get; init; }
            [JsonProperty(Field.Contents)] public ChestContentsData? Contents { get; init; }
        }

        private sealed record ChestAccessData
        {
            [JsonProperty(Field.Mode)] public string? Mode { get; init; }
        }

        private sealed record ChestContentsData
        {
            [JsonProperty(Field.Mode)] public string? Mode { get; init; }
            [JsonProperty(Field.Items)] public List<ChestItemData?>? Items { get; init; }
        }

        private sealed record ChestItemData
        {
            [JsonProperty(Field.SlotId)] public string? SlotId { get; init; }
            [JsonProperty(Field.ItemId)] public string? ItemId { get; init; }
            [JsonProperty(Field.Amount)] public int? Amount { get; init; }
            [JsonProperty(Field.Rarity)] public string? Rarity { get; init; }
        }
    }
}
