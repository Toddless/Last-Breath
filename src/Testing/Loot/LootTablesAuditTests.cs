namespace LastBreathTest.Loot
{
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Data.LootTable;
    using Core.Data.Validation;
    using LastBreathTest.Descriptors;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Catalogs.Checks;
    using Tooling.Schema.Model;

    /// <summary>Every position of the shipped loot tables must resolve to something the game can
    /// actually mint — a position naming an id needs a catalog entry, a position naming a set of
    /// augments needs at least one augment answering its filter. Either way an unresolvable position
    /// is a hard failure: the drop pipeline would burn budget on a seat that produces nothing.
    /// <para>The shape of a position is the checks' own rule, run over the shipped catalogs through the
    /// same entry point the authoring tool presses its button on. It stays a GATE here — the tool reports
    /// and the game refuses.</para>
    /// <para>The ids are held here and not taken from the checks, because a position may name a drop out
    /// of five catalogs and one of them has no describer: the checks leave such a mention unanswered
    /// WHOLE rather than call half of it broken, which is right for a report and would be a gate holding
    /// nothing. So the ids are put to the four catalogs this build can read, the way the gate did before
    /// the checks existed — the fifth is pinned below as the reason the answer is not complete.</para></summary>
    [TestClass]
    public class LootTablesAuditTests
    {
        /// <summary>The key a loot table position writes the one thing it drops under.</summary>
        private const string IdProperty = "id";

        /// <summary>A drop named after nothing any catalog of the game holds.</summary>
        private const string ForgedDropId = "Weapon_Nobody_Wrote";

        /// <summary>The filter the forged seat asks for, named the way a finding names it.</summary>
        private const string ForgedBand = "tier 99 / Common";

        private const string ForgedGroupTableJson = """
        {
            "general": [
                {
                    "key": "forged",
                    "tiers": [
                        {
                            "tier": 0,
                            "items": [ { "augments": { "tier": 99, "rarity": "Common" }, "price": 100 } ]
                        }
                    ]
                }
            ]
        }
        """;

        [TestMethod]
        public void EveryLootTableIdExistsInTheCatalogs()
        {
            List<string> named = [.. Ids(Positions())];

            Assert.AreNotEqual(0, named.Count, "the shipped tables name no drop at all — the gate holds nothing");

            string[] orphans = [.. Findings(CatalogFindingKind.UnknownReference), .. Unanswered(named)];

            Assert.AreEqual(0, orphans.Length, $"Loot table ids without a catalog entry: {string.Join(", ", orphans)}");
        }

        /// <summary>The mutation the gate exists for: a seat naming a drop nobody wrote is one the mint
        /// hands nothing back for, and the budget is spent on it all the same. The shipped drop beside it
        /// has to stay unremarked — a gate that named both would be gating that a word was written.</summary>
        [TestMethod]
        public void APositionNamingADropNobodyWrote_IsCaught()
        {
            List<string> named = [.. Ids(Positions())];

            Assert.AreNotEqual(0, named.Count, "the shipped tables name no drop at all — the case proves nothing");

            string[] unanswered = [.. Unanswered([ForgedDropId, named[0]])];

            CollectionAssert.AreEqual(
                new[] { ForgedDropId },
                unanswered,
                $"the forged seat and the shipped one beside it were read as: {string.Join(", ", unanswered)}");
        }

        /// <summary>The ids of the drops, put to every catalog a position may name them out of that this
        /// build can read. A target with no describer is left out rather than counted as no ids at all:
        /// that one is the pin below, and reading it as empty would call every id of it broken.</summary>
        private static IEnumerable<string> Unanswered(IEnumerable<string> ids)
        {
            var references = new ReferenceIndex(CatalogCrossCheckTests.Shipped.Workspace);
            ReferenceTarget[] described = [.. Targets().Where(target => references.Undescribed([target]).Count == 0)];

            Assert.AreNotEqual(0, described.Length, "no catalog a loot position names is described — the gate holds nothing");

            return ids
                .Where(id => !references.Exists(described, id))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);
        }

        /// <summary>Where a position's id may point, read off the SCHEMA the checks read the tables by.
        /// The markup on the DTO is read once, by the describer that built that schema — a second reader
        /// of the same attributes here would be a second answer to keep in step — and a sixth catalog
        /// written onto the field joins this gate without anyone remembering it.</summary>
        private static IEnumerable<ReferenceTarget> Targets()
        {
            CatalogSchema schema = CatalogCrossCheckTests.Shipped.Workspace.Catalogs
                .Single(view => view.Catalog == DataCatalog.LootTables)
                .Schema;

            foreach (SectionSchema section in schema.Sections)
                if (Drop(section.Record) is { } id)
                    return id.RefTargets;

            throw new AssertFailedException("the loot tables state no shape naming one drop — the gate holds nothing");
        }

        /// <summary>The field naming the one thing a position drops, wherever the shape carrying it stands
        /// in the schema: a seat sits two arrays below a table, and how deep that is belongs to the
        /// describer rather than to a walk written out here.</summary>
        private static FieldSchema? Drop(RecordSchema record)
        {
            if (record.Variants is { } variants)
                foreach (VariantSchema variant in variants.Variants)
                {
                    if (string.Equals(variant.DiscriminatorValue, IdProperty, StringComparison.Ordinal))
                        return Named(variant.Record, IdProperty);

                    if (Drop(variant.Record) is { } worn) return worn;
                }

            foreach (FieldSchema field in record.Fields)
                if (Drop(field) is { } found) return found;

            return null;
        }

        /// <summary>What a field holds, when it holds a record at all: an object is one, and a list or a
        /// map is one repeated.</summary>
        private static FieldSchema? Drop(FieldSchema field) =>
            field.Record is { } record ? Drop(record) : field.Item is { } item ? Drop(item) : null;

        private static FieldSchema? Named(RecordSchema record, string jsonName)
        {
            foreach (FieldSchema field in record.Fields)
                if (string.Equals(field.JsonName, jsonName, StringComparison.Ordinal))
                    return field;

            return null;
        }

        /// <summary>The drop each position names, of the positions that name one at all: the other shape
        /// of a seat names a set of augments and is answered further down.</summary>
        private static IEnumerable<string> Ids(IEnumerable<JObject> positions) =>
            positions.Select(position => (string?)position[IdProperty]).OfType<string>().Where(id => id.Length > 0);

        /// <summary>A position names exactly one of the two kinds of drop. Naming both leaves what
        /// drops undecided, naming neither prices a seat that describes nothing — the parser refuses
        /// either, so a shipped table written that way would lose the line silently.</summary>
        [TestMethod]
        public void EveryLootTablePositionNamesOneKindOfDrop()
        {
            string[] confused = [.. Findings(CatalogFindingKind.AmbiguousShape)];

            Assert.AreEqual(0, confused.Length,
                $"Loot table positions naming both an id and a group, or neither: {string.Join(", ", confused)}");
        }

        /// <summary>What the checks cannot say about the ids of the loot tables, and why the gate above
        /// puts them to the catalogs itself. The legacy plain items have no describer yet
        /// (<c>CatalogDescriptors.NotYetDescribed</c>) and a position may name one of them, so the checks
        /// leave EVERY position id unanswered rather than calling it broken: no <c>UnknownReference</c> can
        /// arrive out of the loot tables while that holds, and the gate stands on the ids it puts to the
        /// four described catalogs on its own. The pin says so out loud — the day that catalog is described
        /// the two halves change places.
        /// <para>The band rule below reads the same tables and is no exception to this: what the game's own
        /// rules find comes back under <see cref="CatalogFindingKind.Rule"/>, so the sort the gate above
        /// reads stays about the ids alone.</para></summary>
        [TestMethod]
        public void TheIdsOfALootPositionAreLeftUnansweredWhileTheItemsCatalogIsUndescribed()
        {
            Assert.IsTrue(
                CatalogCrossCheckTests.Shipped.Findings.Any(finding =>
                    finding.Kind == CatalogFindingKind.UndescribedTarget && finding.Named == DataCatalog.Items),
                "the Items catalog is described now — this pin and the gate above have changed places");
        }

        /// <summary>A group is expanded at mint time, so a filter no augment answers is not a load
        /// error at all — it is a kill that quietly drops one item fewer, forever. The rule is the game's
        /// own (<see cref="LootBandRules"/>), which the authoring tool reads over the documents an author
        /// has open; it stays a GATE here.</summary>
        [TestMethod]
        public void EveryLootTableGroupIsAnsweredByAShippedAugment()
        {
            IReadOnlyList<LootTableSeats> tables = ShippedTables();
            IReadOnlyList<AbilityAugmentData> augments = ShippedAugments();

            Assert.AreNotEqual(0, augments.Count, "the shipped catalogs hold no augment at all — the gate holds nothing");
            Assert.IsTrue(Seats(tables).Any(position => position.Augments is not null),
                "no shipped seat names a set of augments — the gate holds nothing");

            IReadOnlyList<DataFinding> unanswered = LootBandRules.Check(tables, augments);

            Assert.AreEqual(0, unanswered.Count,
                $"Loot table groups no shipped augment answers: {string.Join(", ", unanswered.Select(finding => finding.Named))}");
        }

        /// <summary>The mutation the gate exists for: a seat asking for a tier nothing is written at.
        /// Membership is COVERAGE and not equality, so the band's own edges have to hold — a filter at
        /// either end of a declared band is answered, and one a step outside it is not.</summary>
        [TestMethod]
        public void ASeatOutsideEveryDeclaredBand_IsCaught()
        {
            AbilityAugmentData band = new()
            {
                Id = "Forged_Augment",
                Tier = 1,
                MinRarity = Core.Enums.Rarity.Rare,
                MaxRarity = Core.Enums.Rarity.Epic
            };

            Assert.AreEqual(0, Asked(band, 1, Core.Enums.Rarity.Rare).Count, "the worst end of the band went unanswered");
            Assert.AreEqual(0, Asked(band, 1, Core.Enums.Rarity.Epic).Count, "the best end of the band went unanswered");
            Assert.AreEqual(1, Asked(band, 1, Core.Enums.Rarity.Uncommon).Count, "a rarity below the band was answered");
            Assert.AreEqual(1, Asked(band, 1, Core.Enums.Rarity.Legendary).Count, "a rarity above the band was answered");
            Assert.AreEqual(1, Asked(band, 2, Core.Enums.Rarity.Rare).Count, "another tier answered the seat");
        }

        /// <summary>The same rule over the documents the authoring tool has open: a seat written into the
        /// loot tables that nothing answers reaches the panel's Check as a finding of its own.</summary>
        [TestMethod]
        public void AnUnansweredSeatInTheDocuments_ReachesTheChecksOfTheTool()
        {
            IReadOnlyList<CatalogFinding> added = CatalogCrossCheckTests.Added(DataCatalog.LootTables, ForgedGroupTableJson);

            CollectionAssert.AreEqual(
                new[] { ForgedBand },
                added.Select(finding => finding.Named).ToArray(),
                $"the forged table was read by the tool's checks as:\n  {CatalogCrossCheckTests.Lines(added)}");
        }

        /// <summary>What one seat comes to against one declared band, both forged.</summary>
        private static IReadOnlyList<DataFinding> Asked(AbilityAugmentData band, int tier, Core.Enums.Rarity rarity) =>
            LootBandRules.Check(
                [new LootTableSeats("Forged", [new LootTableTierData { Tier = 0, Items = [new TableRecord(string.Empty, 1f, new AugmentGroup(tier, rarity))] }])],
                [band]);

        /// <summary>What the checks found in the loot tables of one sort, as a line naming the position.</summary>
        private static IEnumerable<string> Findings(CatalogFindingKind kind) =>
            CatalogCrossCheckTests.Shipped.Findings
                .Where(finding => finding.Kind == kind && finding.Catalog == DataCatalog.LootTables)
                .Select(finding => $"{finding.Record} {finding.Where} {finding.Named}".Trim());

        /// <summary>Every seat of every shipped table, read the way the drop pipeline reads them.</summary>
        private static IEnumerable<TableRecord> Seats(IEnumerable<LootTableSeats> tables) =>
            tables.SelectMany(table => table.Tiers).SelectMany(tier => tier.Items);

        /// <summary>The shipped tables under the keys their own file names them by, put to the same reader
        /// the authoring tool hands its open documents to — a position that reader refuses is one no kill
        /// will ever price, so it is no business of this gate either.</summary>
        private static IReadOnlyList<LootTableSeats> ShippedTables() =>
            [.. CatalogFiles(DataCatalog.LootTables).SelectMany(file => LootTableSeats.From(File.ReadAllText(file)))];

        /// <summary>Every shipped augment record, read by the catalog every composition reads them
        /// through.</summary>
        private static IReadOnlyList<AbilityAugmentData> ShippedAugments()
        {
            var catalog = new AbilityAugmentCatalog();

            foreach (string file in CatalogFiles(DataCatalog.Abilities))
                catalog.Apply(DataCatalog.Abilities, new GameDataFile(Path.GetFileName(file), File.ReadAllText(file)));

            return [.. catalog.All];
        }

        /// <summary>Every file of a catalog, subfolders and all — the way the game and the authoring tool
        /// both walk one: a record filed a folder deeper is a record either of them reads.</summary>
        private static IEnumerable<string> CatalogFiles(string catalog) =>
            Directory.EnumerateFiles(SharedData.Catalog(catalog), "*.json", SearchOption.AllDirectories);

        /// <summary>Every seat of every shipped file as it is WRITTEN — the ids are put to the catalogs
        /// straight out of the json, because a converter that refused a seat would hide the very id this
        /// gate is about.</summary>
        private static IEnumerable<JObject> Positions() =>
            CatalogFiles(DataCatalog.LootTables)
                .Select(file => JObject.Parse(File.ReadAllText(file)))
                .SelectMany(root => root.SelectTokens("$..items[*]"))
                .OfType<JObject>();
    }
}
