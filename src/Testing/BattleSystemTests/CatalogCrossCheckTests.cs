namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Localization;
    using LastBreath.Descriptors;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Catalogs.Checks;
    using Tooling.Json;
    using Tooling.Localization;

    /// <summary>What one run of the checks over the shipped data came to, and the workspace it read.</summary>
    internal sealed record ShippedCatalogs(CatalogWorkspace Workspace, IReadOnlyList<CatalogFinding> Findings);

    /// <summary>
    /// The shipped catalogs held against each other and against the wording beside them: the ids they
    /// name, the names they are found by, the shapes their records are written in, the keys their text is
    /// read under — and what the data MEANS where a shape alone cannot say it, which is the game's own
    /// rules over what its parsers made of the files. The authoring tool presses its Check button on the
    /// very same entry point, so what an author sees in the panel and what this run reports are one answer.
    /// <para>A report and not a gate: what the data owes today is PINNED, so a finding arriving fails and
    /// a finding going away fails just as loudly — the second is how a pin outlives the thing it pinned.
    /// The gates that were gates before this run existed are still gates, in the audits that own them.</para>
    /// </summary>
    [TestClass]
    public class CatalogCrossCheckTests
    {
        /// <summary>The folder of .po files, which sits among the data catalogs and is read like one.</summary>
        private const string LocalizationFolder = "Localization";

        /// <summary>Read once for the whole assembly: the run walks every catalog of the game and every
        /// audit that asks about it is asking the same question.</summary>
        private static readonly Lazy<ShippedCatalogs> s_shipped = new(Read);

        /// <summary>The one effect whose description nobody wrote: its card prints the key.</summary>
        private static readonly string[] s_effectsWithNoDescription = ["Effect_Mana_Flow"];

        /// <summary>The passives the tree hands out that no locale names at all — neither the name on the
        /// node nor the sentence under it, so the wheel prints the raw key twice.</summary>
        private static readonly string[] s_passivesWithNoWording =
        [
            "Passive_Skill_Mana_To_Barrier",
            "Passive_Skill_Meteor",
            "Passive_Skill_Ice_Meteor",
            "Passive_Skill_Chain_Attack",
            "Passive_Skill_Gift_From_The_Goddess",
        ];

        /// <summary>
        /// What the shipped catalogs owe today, one row per fact. Held as the sort of finding, the catalog
        /// and the record it stands in and the word it is about, and nothing else: the wording of a
        /// message is the library's to improve, and WHERE in a file a record happens to sit is its
        /// author's to move.
        /// </summary>
        /// <remarks>
        /// A row is taken out when the data is fixed, never to quiet a run.
        /// <list type="bullet">
        /// <item>The undescribed target is the tool's own gap and not the author's: no descriptor is
        /// written for the legacy plain items (<c>CatalogDescriptors.NotYetDescribed</c>), so every id a
        /// loot table names is left unanswered rather than called broken.</item>
        /// <item>The four recipes mint an equipment template no file declares — the bench would take
        /// payment and hand back nothing.</item>
        /// <item>Two mythic jewellery lines write their roll as a plain number instead of a pair of
        /// bounds: the converter reads it as a range of one, so the file carries two spellings of one
        /// field.</item>
        /// <item>Two item effects answer to one id; whichever the reader keeps, the other is a weight in
        /// a roll that hands out nothing.</item>
        /// <item>The rest is wording the data owes — OWES, and not every key a catalog offers a place
        /// for: an effect's standing card opens from a description writing <c>{@Effect_X}</c> and from
        /// nowhere else, so the describer says the effects catalog does not require it and the forty
        /// cards nobody links to are not rows here.</item>
        /// <item>The rules over what the data MEANS write no row at all today: every audit owning one of
        /// them is a green gate, and each is put to a mutation of its own through this same entry point.</item>
        /// </list>
        /// Two rows are alike where a record owes the same thing twice — the two ranges of one pool — and
        /// that is the fact, not a repetition.
        /// </remarks>
        private static readonly (CatalogFindingKind Kind, string Catalog, string Record, string Named)[] s_known =
        [
            (CatalogFindingKind.UndescribedTarget, "", "", DataCatalog.Items),

            (CatalogFindingKind.UnknownReference, DataCatalog.Recipes, "Recipe_Body_Iron_Bastion", "Body_Iron_Bastion"),
            (CatalogFindingKind.UnknownReference, DataCatalog.Recipes, "Recipe_Helmet_Iron_Bastion", "Helmet_Iron_Bastion"),
            (CatalogFindingKind.UnknownReference, DataCatalog.Recipes, "Recipe_Boots_Iron_Bastion", "Boots_Iron_Bastion"),
            (CatalogFindingKind.UnknownReference, DataCatalog.Recipes, "Recipe_Gloves_Iron_Bastion", "Gloves_Iron_Bastion"),

            (CatalogFindingKind.MalformedRange, DataCatalog.ModifierPools, "Mythic_Jewellery", ValueField),
            (CatalogFindingKind.MalformedRange, DataCatalog.ModifierPools, "Mythic_Jewellery", ValueField),

            (CatalogFindingKind.DuplicateId, DataCatalog.ItemEffects, RepeatedEffectId, RepeatedEffectId),

            (CatalogFindingKind.MissingText, DataCatalog.Traders, UnnamedTraderId, UnnamedTraderId),

            .. Owed(DataCatalog.Effects, s_effectsWithNoDescription, LocalizationService.DescriptionSuffix),
            .. Owed(DataCatalog.PassiveSkills, s_passivesWithNoWording, LocalizedKeyAttribute.NoSuffix),
            .. Owed(DataCatalog.PassiveSkills, s_passivesWithNoWording, LocalizationService.DescriptionSuffix),
        ];

        /// <summary>The one field of a modifier line written as a pair of bounds.</summary>
        private const string ValueField = "value";

        /// <summary>The effect two records of the item-effect catalog answer to.</summary>
        private const string RepeatedEffectId = "Passive_Skill_Regeneration";

        /// <summary>The shop whose name no locale words: the trade window titles itself by the trader's
        /// id, so it opens showing the key.</summary>
        internal const string UnnamedTraderId = "Trader_Ronald";

        /// <summary>One run of the checks over the shipped data, through the same entry point the
        /// authoring tool presses its button on. Shared with the audits that own a gate over one of these
        /// rules, so that the tool, the audits and this pin are one answer.</summary>
        internal static ShippedCatalogs Shipped => s_shipped.Value;

        [TestMethod]
        public void TheShippedCatalogsHoldOnlyTheKnownFindings()
        {
            (CatalogFindingKind Kind, string Catalog, string Record, string Named)[] found =
                [.. Shipped.Findings.Select(finding => (finding.Kind, finding.Catalog, finding.Record, finding.Named))];

            Report("What the checks found in the shipped catalogs", [.. Shipped.Findings.Select(Line)]);

            CollectionAssert.AreEquivalent(
                s_known,
                found,
                $"the shipped catalogs hold other findings than the known ones:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", Shipped.Findings.Select(Line)));
        }

        /// <summary>The run has to be reading something: a workspace that opened no catalog would find
        /// nothing to say and pass forever, and so would one whose locales could not be read.</summary>
        [TestMethod]
        public void TheRunReadsTheCatalogsAndTheLocalesAtAll()
        {
            Assert.AreNotEqual(0, Shipped.Workspace.Catalogs.Count, "the run opened no catalog at all");
            Assert.AreNotEqual(0, Shipped.Workspace.RecordCount, "the run read no record at all");
            Assert.AreEqual(
                CatalogDescriptors.All.Count,
                Shipped.Workspace.Catalogs.Count,
                $"a described catalog could not be read:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", Shipped.Workspace.Report));
        }

        /// <summary>
        /// No shipped augment answers the binding question twice and differently. The checks pass a record
        /// writing several of the three binding keys over, because the game's reader takes them IN ORDER —
        /// but the one pair that is no order at all is refused outright by <c>AugmentFit</c>: a record
        /// claiming every ability WHILE naming one is an augment no socket in the game takes, and the tool
        /// stays quiet about it because this holds it.
        /// </summary>
        [TestMethod]
        public void NoShippedAugmentClaimsEveryAbilityWhileNamingOne()
        {
            List<JObject> augments = [.. Augments()];

            Assert.AreNotEqual(0, augments.Count, "the shipped run holds no augment at all — the pin holds nothing");

            string[] contradicting =
            [
                .. augments
                    .Where(augment => augment.Value<bool>(AbilitiesCatalogDescriptor.AnyAbilityField)
                                      && !string.IsNullOrWhiteSpace(augment.Value<string>(AbilitiesCatalogDescriptor.AbilityField)))
                    .Select(augment => augment.Value<string>(AbilitiesCatalogDescriptor.IdField) ?? string.Empty)
            ];

            Assert.AreEqual(0, contradicting.Length,
                "an augment claims every ability while naming one, which the reader refuses wherever it is held: "
                + string.Join(", ", contradicting));
        }

        /// <summary>Every augment record of the run, read out of the workspace the checks read: the
        /// augments stand in a section of their own beside the abilities they improve.</summary>
        private static IEnumerable<JObject> Augments() =>
            Shipped.Workspace.Catalogs
                .Single(view => view.Catalog == DataCatalog.Abilities)
                .Records
                .Where(record => string.Equals(record.Section, AbilitiesCatalogDescriptor.AugmentsKey, StringComparison.Ordinal))
                .Select(record => record.Token)
                .OfType<JObject>();

        /// <summary>Every locale of the run writes each of its keys once. The one hard failure of the
        /// wording: gettext keeps the first entry under a repeated key, so everything written under the
        /// second is out of the game with nothing said about it.</summary>
        [TestMethod]
        public void NoLocaleOfTheRunWritesAKeyTwice()
        {
            string[] twice =
            [
                .. Shipped.Findings
                    .Where(finding => finding.Kind == CatalogFindingKind.DuplicateKey)
                    .Select(finding => $"{finding.Where}  {finding.Named}")
            ];

            Assert.AreEqual(0, twice.Length,
                $"a locale writes a key twice:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", twice)}");
        }

        /// <summary>What one record owes in every locale, as the pin spells it.</summary>
        private static IEnumerable<(CatalogFindingKind, string, string, string)> Owed(
            string catalog, IEnumerable<string> records, string suffix) =>
            records.Select(record => (CatalogFindingKind.MissingText, catalog, record, record + suffix));

        /// <summary>What the file a forged record is laid into is called. Named after nothing the catalogs
        /// hold, so a finding out of it is told from the shipped ones by its address alone.</summary>
        private const string ForgedFileName = "Forged.json";

        /// <summary>
        /// What the checks say about one forged file laid into the shipped catalogs. The file is laid into
        /// the run rather than read on its own, because that is the only way the question the rules exist
        /// for can be asked at all: whether an id answers to anything is a question about every OTHER
        /// catalog, and a bench holding one file would call every word in it broken.
        /// </summary>
        /// <remarks>Only what the forged file itself owes comes back: everything else the run has to say
        /// is what the shipped data already owed, and it is pinned above. The run is its own — a file laid
        /// into the shared one would be there for every audit after it.</remarks>
        internal static IReadOnlyList<CatalogFinding> Forged(string catalog, string json) =>
            [.. Laid(catalog, json).Where(finding => finding.Where.StartsWith(ForgedFileName, StringComparison.Ordinal))];

        /// <summary>
        /// Everything one forged file ADDS to what the run already had to say. Held against a run with no
        /// forged file in it rather than picked out by address, because the rules reading the catalogs
        /// through the game's own parsers answer about a pool, a table or a quest — the file a record was
        /// typed into is not what such a finding is about, and several of them stand at no file at all.
        /// </summary>
        /// <remarks>The rules over the MEANING of the data are the only ones a mutation can be put to this
        /// way, and putting one to them through this entry point is what proves the authoring tool reads
        /// the same rule over the same catalogs as the audit that owns the gate.</remarks>
        internal static IReadOnlyList<CatalogFinding> Added(string catalog, string json)
        {
            List<CatalogFinding> before = [.. s_bare.Value];
            List<CatalogFinding> added = [];

            // Struck off one at a time rather than filtered against the list: two findings alike are two
            // facts, and asking a list whether it holds one at all would drop both or keep both.
            foreach (CatalogFinding finding in Laid(catalog, json))
                if (!before.Remove(finding))
                    added.Add(finding);

            return added;
        }

        /// <summary>One run over the shipped catalogs with one forged file laid into them. The wording is
        /// left unread: a run held against another run has to differ by the forged file alone.</summary>
        private static IReadOnlyList<CatalogFinding> Laid(string catalog, string json)
        {
            var workspace = CatalogWorkspace.Load(SharedData.Root(), CatalogDescriptors.All);
            CatalogView view = workspace.Catalogs.Single(open => open.Catalog == catalog);

            view.AddFile(new CatalogFile(Path.Combine(view.Folder, ForgedFileName), JsonTreeDocument.Parse(json)));

            return CatalogCheckRun.Over(workspace, new ReferenceIndex(workspace), texts: null);
        }

        /// <summary>A resource whose id was typed before its material was — what a record looks like
        /// halfway through being written.</summary>
        private const string HalfWrittenResourceJson = """
        {
            "craftingResources": [ { "id": "Resource_Half_Written", "maxStackSize": 1, "tags": [], "rarity": "Common" } ]
        }
        """;

        /// <summary>
        /// A record halfway through being written reaches the game's own parsers as a field that is not
        /// there yet. This run is a REPORT, pressed over documents being typed into: such a record costs
        /// its own family's answer and nothing else — not the families beside it, and not the shape rules
        /// spread into the same list — so the button an author presses answers instead of throwing.
        /// </summary>
        [TestMethod]
        public void AHalfWrittenRecordDoesNotTakeTheRunDown()
        {
            IReadOnlyList<CatalogFinding> added = Added(DataCatalog.Resources, HalfWrittenResourceJson);

            Assert.AreEqual(0, added.Count(finding => finding.Kind == CatalogFindingKind.Rule),
                $"a resource with no material was read as a finding of the game's own rules:{Environment.NewLine}  {Lines(added)}");
        }

        /// <summary>What the shipped catalogs come to with no forged file and no wording read — the run
        /// every mutation is held against. Read once for the whole assembly, the way the shipped run is.</summary>
        private static readonly Lazy<IReadOnlyList<CatalogFinding>> s_bare = new(Bare);

        private static IReadOnlyList<CatalogFinding> Bare()
        {
            var workspace = CatalogWorkspace.Load(SharedData.Root(), CatalogDescriptors.All);

            return CatalogCheckRun.Over(workspace, new ReferenceIndex(workspace), texts: null);
        }

        /// <summary>The words one sort of finding is about, in the order the run met them.</summary>
        internal static string[] Words(IReadOnlyList<CatalogFinding> found, CatalogFindingKind kind) =>
            [.. found.Where(finding => finding.Kind == kind).Select(finding => finding.Named)];

        internal static string Lines(IReadOnlyList<CatalogFinding> found) =>
            found.Count == 0
                ? "nothing at all"
                : string.Join($"{Environment.NewLine}  ", found.Select(finding => $"{finding.Kind}  {finding.Named}  {finding.Where}"));

        private static ShippedCatalogs Read()
        {
            string root = SharedData.Root();
            var workspace = CatalogWorkspace.Load(root, CatalogDescriptors.All);

            return new ShippedCatalogs(
                workspace,
                CatalogCheckRun.Over(
                    workspace, new ReferenceIndex(workspace), LocalizedTexts.Load(Path.Combine(root, LocalizationFolder))));
        }

        private static string Line(CatalogFinding finding) =>
            $"{finding.Kind}  {finding.Catalog}/{finding.Record}  {finding.Named}  {finding.Where}  —  {finding.Message}";

        private static void Report(string what, IReadOnlyList<string> lines) =>
            Console.WriteLine(lines.Count == 0
                ? $"{what}: none."
                : $"{what} ({lines.Count}):{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", lines)}");
    }
}
