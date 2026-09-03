namespace Tooling.Catalogs.Forms
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;

    /// <summary>
    /// The words a loot table is written with. Named by the host and never spelled here: they belong to
    /// the game's own types, and a form that wrote them down itself would be a second place to keep in
    /// step with the file.
    /// </summary>
    public sealed record LootTableLayout
    {
        /// <summary>What a table holds its tiers under.</summary>
        public required string TiersKey { get; init; }

        /// <summary>What a tier is numbered by.</summary>
        public required string TierKey { get; init; }

        /// <summary>What a tier holds its positions under.</summary>
        public required string ItemsKey { get; init; }

        /// <summary>What a position naming one thing writes it under; the key being there is what picks
        /// that shape.</summary>
        public required string IdKey { get; init; }

        /// <summary>What a position naming a set of augments writes the filter under; the key being
        /// there is what picks that shape.</summary>
        public required string AugmentsKey { get; init; }

        /// <summary>What a position costs in loot units.</summary>
        public required string PriceKey { get; init; }

        /// <summary>Which tier of augments a group names, inside the filter.</summary>
        public required string AugmentTierKey { get; init; }

        /// <summary>Which rarity a group names, inside the filter.</summary>
        public required string RarityKey { get; init; }

        /// <summary>How often a tier comes up, where the game reads such a key on a tier at all; null
        /// where it does not, and nothing is then drawn for it — a box writing a key the game never
        /// reads is worse than no box.</summary>
        public string? ChanceKey { get; init; }
    }

    /// <summary>A set of augments as a position names it: which tier its members are of, and where they
    /// stand on the rarity scale. Either half may be missing — the file is what it is, and a half-written
    /// filter read as a whole one would hide the half.</summary>
    public sealed record LootAugmentGroup(int? Tier, string? Rarity);

    /// <summary>One seat at a tier: where it stands, what the file wrote there, what it names — one thing
    /// by id or a set of augments — and what it costs in loot units.</summary>
    public sealed record LootPosition(JsonPointer At, JToken? Token, string? Id, LootAugmentGroup? Augments, double? Price)
    {
        /// <summary>Whether the seat names exactly one of the two things a seat may name. Naming both
        /// leaves what drops undecided and naming neither describes nothing at all: the game drops the
        /// seat either way, so neither is drawn as though it were a position.</summary>
        public bool NamesOneThing => (Id is not null) ^ (Augments is not null);

        /// <summary>Whether the game will let the seat take budget: a seat is bought for a positive
        /// number of loot units, and one costing nothing would be bought over and over for free.</summary>
        public bool Priced => Price is > 0;
    }

    /// <summary>One tier of a table: where it stands, the number it is found by, how often it comes up
    /// where that is written beside it, and the seats at it.</summary>
    public sealed record LootTier(JsonPointer At, int? Tier, double? Chance, IReadOnlyList<LootPosition> Positions);

    /// <summary>What a tier comes to: how many seats stand at it, what they cost together, and how many
    /// of them the game will refuse to buy.</summary>
    public readonly record struct LootTierSummary(int Positions, double Price, int Unpriced);

    /// <summary>What the schema says about the two values of a position a form cannot invent: which
    /// catalogs an id may point into, and the rarities a group may name.</summary>
    public sealed record LootPositionSchema(IReadOnlyList<ReferenceTarget> Drops, IReadOnlyList<string> Rarities);

    /// <summary>
    /// A loot table read the way its author edits it — tiers, and the seats standing at each of them —
    /// together with the handful of changes that are gestures rather than fields: a seat written, a seat
    /// moved to another tier, a tier laid down or taken out.
    /// <para>Every one of them is ONE step of the history. A move is the whole list of tiers written back
    /// rather than a removal and an insertion: the two halves of it are a seat nowhere and a seat twice
    /// over, and a file left between them is a table nobody asked for.</para>
    /// <para>Godot-free and game-free like the rest of what a host draws from: the words a table is
    /// written with arrive as a <see cref="LootTableLayout"/>, and nothing here knows what an augment is.</para>
    /// </summary>
    public sealed class LootTableForm(LootTableLayout layout)
    {
        /// <summary>The tier a table with none opens at. Tier 0 is the best and every table has one.</summary>
        private const int FirstTier = 0;

        public LootTableLayout Layout { get; } = layout ?? throw new ArgumentNullException(nameof(layout));

        /// <summary>The tiers of one table, in the order the file writes them. Nothing at all where the
        /// record holds no list of tiers: a table with none is a table nothing drops from, which the host
        /// says out loud rather than drawing an empty block for.</summary>
        public IReadOnlyList<LootTier> Tiers(JToken? record, JsonPointer at)
        {
            ArgumentNullException.ThrowIfNull(at);

            List<LootTier> read = [];

            if (record is not JObject table || table[Layout.TiersKey] is not JArray tiers) return read;

            JsonPointer tiersAt = at.Append(Layout.TiersKey);

            for (int index = 0; index < tiers.Count; index++) read.Add(Tier(tiers[index], tiersAt.Append(index)));

            return read;
        }

        /// <summary>What the schema says about a position, walked down from the table's own record: a
        /// catalog nobody described and a shape nobody registered answer with nothing, which leaves the
        /// host a box to type into instead of a list to pick from.</summary>
        public LootPositionSchema Describe(RecordSchema table)
        {
            ArgumentNullException.ThrowIfNull(table);

            RecordSchema? position = Holding(Holding(table, Layout.TiersKey), Layout.ItemsKey);
            FieldSchema? id = Named(position, Layout.IdKey);
            FieldSchema? rarity = Named(Holding(position, Layout.AugmentsKey), Layout.RarityKey);

            return new LootPositionSchema(
                id is { } drop ? [.. drop.RefTargets] : [],
                rarity is { } scale ? [.. rarity.EnumValues] : []);
        }

        /// <summary>What one tier comes to. Counted rather than remembered: the tier is read out of the
        /// document on every draw, and a total worked out anywhere else would be answering for the file
        /// as it was.</summary>
        public static LootTierSummary Summary(LootTier tier)
        {
            ArgumentNullException.ThrowIfNull(tier);

            double price = 0;
            int unpriced = 0;

            foreach (LootPosition position in tier.Positions)
            {
                price += position.Price ?? 0;

                if (!position.Priced) unpriced++;
            }

            return new LootTierSummary(tier.Positions.Count, price, unpriced);
        }

        /// <summary>The ids one tier writes more than once, each of them named once. A thing seated twice
        /// at one tier is not refused — a designer may mean it — but it takes two shares of the draw, and
        /// nothing else in the file says so.</summary>
        public static IReadOnlyList<string> Repeated(LootTier tier)
        {
            ArgumentNullException.ThrowIfNull(tier);

            HashSet<string> seen = new(StringComparer.Ordinal);
            HashSet<string> twice = new(StringComparer.Ordinal);
            List<string> repeated = [];

            foreach (LootPosition position in tier.Positions)
            {
                if (position.Id is not { Length: > 0 } id) continue;

                // Named on the second seat and not on the third: the answer is which ids stand twice,
                // and one written five times is still one line for the author to read.
                if (!seen.Add(id) && twice.Add(id)) repeated.Add(id);
            }

            return repeated;
        }

        /// <summary>The number a tier laid down now would carry: the first one no tier of the table has.
        /// A table merges its tiers by that number, so two tiers under one of them are one tier the author
        /// edits in two places.</summary>
        public static int NextTier(IReadOnlyList<LootTier> tiers)
        {
            ArgumentNullException.ThrowIfNull(tiers);

            HashSet<int> written = [];

            foreach (LootTier tier in tiers)
                if (tier.Tier is { } number)
                    written.Add(number);

            int next = FirstTier;

            while (written.Contains(next)) next++;

            return next;
        }

        /// <summary>
        /// Writes a tier into the table, in the place its number gives it: before the first tier written
        /// under a bigger one. Tier 0 is the best and the list is read as a ladder, so a tier laid down at
        /// the end would put the number the author just wrote under the ones it outranks.
        /// <para>The tier arrives with the empty list of seats it is edited through: a key the author
        /// would have to add before he could add anything to it is a gesture with nothing behind it.</para>
        /// </summary>
        public bool AddTier(JsonTreeDocument document, JsonPointer record, int tier)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(record);

            if (document.Resolve(record) is not JObject) return false;

            JObject written = new()
            {
                [Layout.TierKey] = tier,
                [Layout.ItemsKey] = new JArray()
            };

            JsonPointer at = record.Append(Layout.TiersKey);

            return document.Resolve(at) is JArray tiers
                ? document.Insert(at, Place(tiers, tier), written)
                : document.Insert(record, Layout.TiersKey, new JArray(written));
        }

        /// <summary>Takes a tier out of its table, seats and all.</summary>
        public static bool RemoveTier(JsonTreeDocument document, JsonPointer tier)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(tier);

            return document.Remove(tier);
        }

        /// <summary>Seats one thing at a tier, by the id the author picked and for what it costs. The
        /// keys are written in the order the game's own writer writes them, so a seat added by hand and a
        /// seat written by the game read as one line.</summary>
        public bool AddPosition(JsonTreeDocument document, JsonPointer tier, string id, double price)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(tier);
            ArgumentNullException.ThrowIfNull(id);

            JObject seat = new()
            {
                [Layout.IdKey] = id,
                [Layout.PriceKey] = Price(price)
            };

            return Append(document, tier, Layout.ItemsKey, seat);
        }

        /// <summary>Seats a SET of augments at a tier: one seat for the whole set, which is what keeps
        /// its share of the draw a decision rather than a count of what answers the filter.</summary>
        public bool AddGroup(JsonTreeDocument document, JsonPointer tier, int augmentTier, string rarity, double price)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(tier);
            ArgumentNullException.ThrowIfNull(rarity);

            JObject seat = new()
            {
                [Layout.PriceKey] = Price(price),
                [Layout.AugmentsKey] = new JObject
                {
                    [Layout.AugmentTierKey] = augmentTier,
                    [Layout.RarityKey] = rarity
                }
            };

            return Append(document, tier, Layout.ItemsKey, seat);
        }

        /// <summary>Takes one seat out of its tier.</summary>
        public static bool RemovePosition(JsonTreeDocument document, JsonPointer position)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(position);

            return document.Remove(position);
        }

        /// <summary>
        /// Moves a seat to another tier of the same table, whole: what it names and what it costs travel
        /// with it, because a price belongs to the seat and not to the tier it happened to stand in.
        /// <para>The list of tiers is written back in one step. Both halves of a move address the same
        /// list, and an undo that put the seat back without taking away the copy would leave the table
        /// holding it twice.</para>
        /// <para>False when either address holds nothing, when the two tiers are not tiers of one table,
        /// and when the seat is already at the tier it is being moved to.</para>
        /// </summary>
        public bool MovePosition(JsonTreeDocument document, JsonPointer position, JsonPointer tier)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(position);
            ArgumentNullException.ThrowIfNull(tier);

            if (position.Parent is not { } seats || seats.Parent is not { } leaving) return false;
            if (leaving == tier || seats.Last != Layout.ItemsKey) return false;
            if (tier.Parent is not { } tiers || leaving.Parent != tiers) return false;

            if (document.Resolve(tiers) is not JArray written) return false;
            if (!Index(leaving, written.Count, out int from) || !Index(tier, written.Count, out int to)) return false;
            if (!JsonPointer.TryReadIndex(position.Last!, out int seat)) return false;

            var moved = (JArray)written.DeepClone();

            if (moved[from] is not JObject source || source[Layout.ItemsKey] is not JArray held) return false;
            if (seat >= held.Count || moved[to] is not JObject target) return false;

            JToken taken = held[seat];
            taken.Remove();

            if (target[Layout.ItemsKey] is JArray standing) standing.Add(taken);
            else target[Layout.ItemsKey] = new JArray(taken);

            return document.SetValue(tiers, moved);
        }

        /// <summary>Writes a value at the end of a list one of the record's keys holds, laying the list
        /// down where the record has none. False where the key holds something that is not a list: what
        /// the author wrote there is not this form's to overwrite.</summary>
        private static bool Append(JsonTreeDocument document, JsonPointer record, string key, JToken value)
        {
            if (document.Resolve(record) is not JObject) return false;

            JsonPointer at = record.Append(key);

            return document.Resolve(at) is JArray written
                ? document.Insert(at, written.Count, value)
                : document.Insert(record, key, new JArray(value));
        }

        /// <summary>Where a tier of that number belongs among the ones already written: before the first
        /// standing under a bigger number, and at the end where none does. A tier the file left unnumbered
        /// is stepped over rather than ranked — nothing can be said about where it belongs.</summary>
        private int Place(JArray tiers, int tier)
        {
            for (int index = 0; index < tiers.Count; index++)
                if (Whole(tiers[index] as JObject, Layout.TierKey) is { } number && number > tier)
                    return index;

            return tiers.Count;
        }

        /// <summary>Where a tier stands in the list of them, when it stands in it at all.</summary>
        private static bool Index(JsonPointer tier, int count, out int index) =>
            JsonPointer.TryReadIndex(tier.Last!, out index) && index < count;

        /// <summary>A price as the file spells prices: a whole number where the author gave one, because
        /// that is what the game's writer and every shipped table already hold.</summary>
        private static JValue Price(double price) => JsonScalars.Number(previous: null, price, whole: true);

        private LootTier Tier(JToken token, JsonPointer at)
        {
            var holder = token as JObject;
            List<LootPosition> positions = [];

            if (holder?[Layout.ItemsKey] is JArray seats)
            {
                JsonPointer seatsAt = at.Append(Layout.ItemsKey);

                for (int index = 0; index < seats.Count; index++) positions.Add(Position(seats[index], seatsAt.Append(index)));
            }

            return new LootTier(at, Whole(holder, Layout.TierKey), Chance(holder), positions);
        }

        private double? Chance(JObject? tier) => Layout.ChanceKey is { } key ? Number(tier, key) : null;

        private LootPosition Position(JToken token, JsonPointer at)
        {
            var holder = token as JObject;

            return new LootPosition(at, token, Text(holder, Layout.IdKey), Group(holder), Number(holder, Layout.PriceKey));
        }

        /// <summary>The filter a seat names, or nothing where it names none. A key holding something that
        /// is not a filter is not read as one: the seat then names neither of the two things a seat may
        /// name, which is what the host shows the author instead of an empty pair of boxes.</summary>
        private LootAugmentGroup? Group(JObject? position) =>
            position?[Layout.AugmentsKey] is JObject filter
                ? new LootAugmentGroup(Whole(filter, Layout.AugmentTierKey), Text(filter, Layout.RarityKey))
                : null;

        /// <summary>The record a key holds one of, whatever stands between — a list of them, a map of
        /// them. Null where the schema names no such key, or names one holding no records.</summary>
        private static RecordSchema? Holding(RecordSchema? record, string jsonName)
        {
            FieldSchema? field = Named(record, jsonName);

            while (field is { Record: null, Item: { } item }) field = item;

            return field?.Record;
        }

        private static FieldSchema? Named(RecordSchema? record, string jsonName)
        {
            if (record is null) return null;

            foreach (FieldSchema field in record.Fields)
                if (string.Equals(field.JsonName, jsonName, StringComparison.Ordinal))
                    return field;

            return null;
        }

        /// <summary>A word the file wrote, spelled the way the file spells it; null for a key it does not
        /// hold at all — an absent key and an empty word are two different things to an author.</summary>
        private static string? Text(JObject? holder, string key) =>
            holder?[key] is JValue { Value: not null } value ? JsonScalars.Written(value) : null;

        private static int? Whole(JObject? holder, string key) =>
            holder?[key] is JValue { Type: JTokenType.Integer } value ? value.Value<int>() : null;

        private static double? Number(JObject? holder, string key) =>
            holder?[key] is JValue { Type: JTokenType.Integer or JTokenType.Float } value ? value.Value<double>() : null;
    }
}
