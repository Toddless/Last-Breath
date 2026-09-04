namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Crafting;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Data.GameData;
    using Core.Data.QuestData;
    using Core.Data.Validation;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using LastBreath.Descriptors.Preview;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Catalogs.Checks;
    using Tooling.Localization;
    using Tooling.Schema.Model;

    /// <summary>
    /// Every cross-check over the documents a tool has open, in one press. Two sets of rules run here and
    /// neither is written here: the shape rules are the library's (<see cref="CatalogChecks"/>) and read a
    /// schema and a document, and the rules that need the MEANING of the data are the game's own
    /// (<c>Core.Data.Validation</c>) and read what the game's parsers made of it.
    /// <para>The one place both answers are assembled. The authoring tool presses a button on it and the
    /// game's tests run it over the shipped data, so what an author sees in the Checks panel and what a
    /// run reports are one answer.</para>
    /// </summary>
    public static class CatalogCheckRun
    {
        /// <summary>What a requirement writes the kind of thing it asks for under.</summary>
        private const string RequirementTypeField = "type";

        /// <summary>What a grant writes the kind of behaviour it hands over under.</summary>
        private const string GrantKindField = "kind";

        /// <summary>The catalogs an item a quest may hand over is read out of by the item provider — the
        /// equipment templates, the recipes and the resources. The ornaments have a catalog of their own
        /// and are read beside these.
        /// <para>The legacy plain items (<see cref="DataCatalog.Items"/>) are the one place an item may be
        /// written that this run does not ask: no reader of this build can build one. A quest handing out
        /// such an id is reported undeclared, which is the truth about this build and is pinned in the
        /// audit that owns the gate.</para></summary>
        private static readonly string[] s_itemCatalogs = [DataCatalog.EquipItems, DataCatalog.Recipes, DataCatalog.Resources];

        /// <summary>Runs every rule over the catalogs as they are written this second.</summary>
        public static IReadOnlyList<CatalogFinding> Over(
            CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts) =>
        [
            .. CatalogChecks.Run(workspace, references, texts, NamesNoRecord, ReadsInOrder),
            .. Meant(workspace).Select(Written),
        ];

        /// <summary>
        /// Records whose shapes the game reads IN ORDER rather than as an exclusive set. An augment binds
        /// by the first of the three keys it writes, in the order the reader takes them — the claim on
        /// every ability, then the ability it names, then its tags — and every record carries tags
        /// whichever way it binds, so a record answering by claim or by name and carrying tags beside it
        /// is written exactly as it is meant to be.
        /// <para>The one pair that is no order at all is refused rather than read first: a record claiming
        /// every ability WHILE naming one answers the question twice and differently, and the reader
        /// (<c>AugmentFit</c>) turns it down wherever it is held. That the shipped data writes no such
        /// record is pinned beside these rules and not answered here.</para>
        /// </summary>
        private static bool ReadsInOrder(RecordSchema record) =>
            string.Equals(record.TypeName, nameof(AbilityAugmentData), StringComparison.Ordinal);

        /// <summary>
        /// Records whose id is not a reference at all. Two cases, and both are the price of a markup that
        /// cannot see the field beside it: a requirement demanding a level of MASTERY writes a
        /// localization key where its siblings write a resource, and a grant handing over a MODIFIER names
        /// no record anywhere — its id is the label its own minted lines carry. The markup states the
        /// catalogs without regard to the field that says which, so the shipped word answering nothing
        /// there is the limit of the contract and not broken data.
        /// </summary>
        private static bool NamesNoRecord(JObject holder)
        {
            ArgumentNullException.ThrowIfNull(holder);

            return string.Equals(holder.Value<string>(RequirementTypeField), nameof(RequirementType.MasteryLevel), StringComparison.Ordinal)
                   || string.Equals(holder.Value<string>(GrantKindField), nameof(GrantKind.Modifier), StringComparison.Ordinal);
        }

        // ── the rules that read what the data MEANS ────────────────────────────────────────────────

        /// <summary>
        /// The game's own rules over the documents, each family read through the game's own parsers. Every
        /// one of them is guarded on its own: the documents are being typed into, and a half-written pool
        /// must not take the rules beside it — or the shape rules above — down with it.
        /// </summary>
        /// <remarks>A catalog this run never opened has nothing said about it here. The rules of the run
        /// itself already answer for a catalog nobody described, once for the whole run, and a second
        /// saying of it per family would be one row per rule about one gap.</remarks>
        private static IEnumerable<DataFinding> Meant(CatalogWorkspace workspace)
        {
            var parser = new DataParser(new PreviewItemFactory());

            return
            [
                .. Guarded(() => ContextKnobRules.Check(Pools(workspace, parser))),
                .. Guarded(() => LootBandRules.Check(Tables(workspace), Augments(workspace))),
                .. Guarded(() => Judged(workspace, parser)),
            ];
        }

        /// <summary>
        /// One family read, or nothing at all where the documents could not be read as the game reads them.
        /// Silent by design: a report is pressed over documents mid-edit, and a rule that cannot read them
        /// yet has nothing to say rather than something to complain about.
        /// </summary>
        /// <remarks>Everything is caught and not a named list of failures. This is the REPORTING path: a
        /// record half typed reaches the game's own parsers as a field that is not there yet, and a rule
        /// falling over one of them must not take the families beside it — nor the shape rules, which are
        /// spread into the same answer — down with it. The gates over the shipped data are elsewhere and
        /// are where a rule failing is a failure.</remarks>
        private static IReadOnlyList<DataFinding> Guarded(Func<IReadOnlyList<DataFinding>> rule)
        {
            try
            {
                return rule();
            }
            catch (Exception)
            {
                return [];
            }
        }

        /// <summary>What the quests hand out, held against the item catalogs — and nothing at all where one
        /// of those catalogs came back empty. An id is called undeclared by every catalog that could
        /// declare it having been read; a folder the run could not read would name every reward of the game
        /// instead, which is a page of rows about one gap.</summary>
        private static IReadOnlyList<DataFinding> Judged(CatalogWorkspace workspace, IDataParser parser) =>
            Items(workspace, parser) is { } items ? QuestRewardRules.Check(Quests(workspace), items) : [];

        /// <summary>Every pool an item's lines are rolled from: the modifier catalog's own pools, the
        /// material categories and the resources carrying descriptors of their own. A reroll unions them,
        /// so for a single item they are one pool.</summary>
        private static IReadOnlyList<RollablePool> Pools(CatalogWorkspace workspace, IDataParser parser)
        {
            List<RollablePool> pools = [];

            foreach (GameDataFile file in Documents(workspace, DataCatalog.ModifierPools))
                foreach ((string id, List<IModifierDescriptor> pool) in parser.ParseEquipItemModifierPools(file.Json))
                    pools.Add(new RollablePool(id, pool));

            foreach (GameDataFile file in Documents(workspace, DataCatalog.Resources))
            {
                List<ICraftingResource> materials =
                [
                    .. parser.ParseResources(file.Json).OfType<ICraftingResource>().Where(resource => resource.Material is not null)
                ];

                foreach (IMaterialCategory category in materials
                    .Select(resource => resource.Material!.MaterialCategory)
                    .OfType<IMaterialCategory>()
                    .DistinctBy(category => category.Id, StringComparer.Ordinal))
                    pools.Add(new RollablePool(category.Id, category.Modifiers));

                foreach (ICraftingResource resource in materials)
                    pools.Add(new RollablePool(resource.Id, resource.Material!.Modifiers));
            }

            return pools;
        }

        /// <summary>Every loot table of the run, under the key its own file names it by — the open
        /// documents put to the one reader in the game that says what a file's tables are.</summary>
        private static IReadOnlyList<LootTableSeats> Tables(CatalogWorkspace workspace) =>
            [.. Documents(workspace, DataCatalog.LootTables).SelectMany(file => LootTableSeats.From(file.Json))];

        /// <summary>Every augment record the run holds, read by the catalog every composition reads them
        /// through.</summary>
        private static IReadOnlyList<AbilityAugmentData> Augments(CatalogWorkspace workspace)
        {
            var catalog = new AbilityAugmentCatalog();

            Read(workspace, DataCatalog.Abilities, catalog);

            return [.. catalog.All];
        }

        /// <summary>Every quest the documents WRITE, whether or not a loader would keep it: a hand-out is
        /// written down in the record, and a record dropped for something else is one an author is still
        /// about to fix.</summary>
        private static IReadOnlyList<QuestEntry> Quests(CatalogWorkspace workspace)
        {
            List<QuestEntry> quests = [];

            foreach (GameDataFile file in Documents(workspace, DataCatalog.Quests))
                if (JsonConvert.DeserializeObject<QuestsData>(file.Json) is { Quests: { } written })
                    quests.AddRange(written);

            return quests;
        }

        /// <summary>What the item catalogs answer about an id a quest hands over, through the game's own
        /// readers: the equip templates, the recipes and the resources, the ornaments, and the one query
        /// that says which of them the world holds exactly one of. Nothing at all where one of the four
        /// came back holding no record: the rule reads silence as "no catalog declares it", so a catalog
        /// the run could not read has to stop the rule rather than answer for it.</summary>
        private static IQuestRewardItems? Items(CatalogWorkspace workspace, IDataParser parser)
        {
            var items = new ItemDataProvider(parser, s_itemCatalogs);
            var ornaments = new OrnamentCatalog();

            foreach (string catalog in s_itemCatalogs) Read(workspace, catalog, items);

            Read(workspace, DataCatalog.Ornaments, ornaments);

            HashSet<string> resources = [.. items.GetAllResources().Select(resource => resource.Id)];
            HashSet<string> recipes = [.. items.GetCraftingRecipes().Select(recipe => recipe.Id)];

            if (!items.AllBlueprints.Any() || resources.Count == 0 || recipes.Count == 0 || ornaments.All.Count == 0) return null;

            var unique = new UniqueItemQuery(items, ornaments);

            return new QuestRewardItems(
                itemId => items.GetBlueprint(itemId) is not null
                          || ornaments.Find(itemId) is not null
                          || resources.Contains(itemId)
                          || recipes.Contains(itemId),
                unique.IsUnique);
        }

        /// <summary>One finding of the game's rules as the panel and the pins read a row. Every one of them
        /// is of the sort the library keeps for a rule of the game, and WHICH rule spoke travels beside it:
        /// borrowing the sorts of the shape rules would file four facts of the game under words that mean
        /// something else — a gate over the loot tables reading its own ids would take a knob's disagreement
        /// for an id nothing answers.</summary>
        private static CatalogFinding Written(DataFinding finding) =>
            new(
                CatalogFindingKind.Rule,
                finding.Catalog,
                finding.Record,
                finding.Named,
                finding.Where,
                finding.Message,
                finding.Kind.ToString());

        /// <summary>The open documents of one catalog. What could not be read is dropped here rather than
        /// noted: this entry point answers in findings alone.</summary>
        private static IReadOnlyList<GameDataFile> Documents(CatalogWorkspace workspace, string catalog) =>
            WorkspaceDocuments.Open(workspace, catalog, new List<string>());

        private static void Read(CatalogWorkspace workspace, string catalog, IGameDataParticipant participant) =>
            WorkspaceDocuments.Read(workspace, catalog, participant, new List<string>());
    }
}
