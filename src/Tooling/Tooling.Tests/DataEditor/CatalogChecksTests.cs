namespace Tooling.Tests.DataEditor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Catalogs.Checks;
    using Tooling.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>
    /// The rules an authoring tool reports on and the game's own tests gate with — one library, run here
    /// over a bench forged to owe one of each: an id nothing answers, a catalog nobody described, two
    /// records under one name, a key no locale words, a translation still to be written, a locale writing
    /// one key twice, a range written as a number and a record wearing two shapes at once.
    /// <para>Each rule is asked against a case that must be found AND a case beside it that must stay
    /// unremarked: a run that reported everything would pass every one of these by accident.</para>
    /// </summary>
    [TestClass]
    public class CatalogChecksTests
    {
        private const string NpcCatalog = "Npc";
        private const string TableCatalog = "LootTables";
        private const string SettingsCatalog = "WorldClock";

        /// <summary>A catalog naming a record the npcs name too, worded under the same suffixes and owing
        /// only the name. Walked FIRST, so that a key one catalog offers and another owes is met by the
        /// one that merely offers it.</summary>
        private const string EchoCatalog = "Echoes";

        /// <summary>A catalog a field points into that no descriptor of this bench covers.</summary>
        private const string PlaceCatalog = "Places";

        private const string NpcsKey = "npcs";
        private const string TablesKey = "general";
        private const string EchoesKey = "echoes";

        private const string IdField = "id";
        private const string AllyField = "ally";
        private const string HomeField = "home";
        private const string StanceField = "stance";
        private const string StancesField = "stances";
        private const string ParametersField = "parameters";
        private const string PriceField = "price";
        private const string ItemsField = "items";
        private const string GroupField = "group";
        private const string MinBound = "min";
        private const string MaxBound = "max";

        private const string NpcFile = "Npc.json";
        private const string TableFile = "LootTables.json";
        private const string SettingsFile = "WorldClock.json";
        private const string EchoFile = "Echoes.json";

        private const string LocalizationFolder = "Localization";

        private const string DescriptionSuffix = "_Description";

        /// <summary>A key the bench's catalog offers a place for and does not owe — the standing card an
        /// author writes for the records something links to and for no others.</summary>
        private const string TooltipSuffix = "_Tooltip";

        /// <summary>The same key with one letter too many — what a required suffix looks like when the
        /// describer misspells one of the suffixes it declares.</summary>
        private const string MisspeltSuffix = "_Tooltips";

        private const string RonaldId = "Npc_Ronald";
        private const string TwinId = "Npc_Twin";
        private const string GhostId = "Npc_Ghost";

        /// <summary>Two npcs answering to one name, one naming an ally nobody wrote, one naming a home in a
        /// catalog nobody described, one writing its band as a plain number instead of a pair of bounds,
        /// and one writing a word outside the members offered in each of the three places a word can stand
        /// — a field of its own, an element of a list and the key of a map. Beside each of them stands the
        /// same thing written right, the first of them in the wrong case: the readers match members
        /// without regard to it and a rule stricter than the reader would report a word the game takes.</summary>
        private const string Npcs = """
            {
                "npcs": [
                    {
                        "id": "Npc_Ronald",
                        "ally": "Npc_Twin",
                        "band": { "min": 1, "max": 3 },
                        "stance": "strength",
                        "stances": [ "Dexterity" ],
                        "parameters": { "Health": 1 }
                    },
                    {
                        "id": "Npc_Twin",
                        "ally": "Npc_Ghost",
                        "home": "Place_Forge",
                        "stance": "Shouter",
                        "stances": [ "Whisper" ],
                        "parameters": { "Mood": 1 }
                    },
                    { "id": "Npc_Twin", "band": 4 }
                ]
            }
            """;

        /// <summary>Four positions: one naming a drop, one naming both a drop and a group, one naming
        /// neither, and one writing the key of a shape as a flag turned off.</summary>
        private const string Tables = """
            {
                "general": [
                    {
                        "key": "Table",
                        "items": [
                            { "id": "Npc_Ronald", "price": 1 },
                            { "id": "Npc_Ronald", "group": "any", "price": 1 },
                            { "price": 1 },
                            { "group": false, "price": 1 }
                        ]
                    }
                ]
            }
            """;

        /// <summary>One record answering to a name the npcs use too. Its catalog offers the description
        /// key and does not owe it, so the key nobody wrote must still be said — for the catalog that owes
        /// it, and not here.</summary>
        private const string Echoes = """
            {
                "echoes": [
                    { "id": "Npc_Twin" }
                ]
            }
            """;

        /// <summary>A document that is one record and names none of itself: nothing here words a key, and
        /// nothing here may be reported as a name written twice.</summary>
        private const string Settings = """
            { "hoursPerDay": 24 }
            """;

        /// <summary>English: Ronald is named and described, the twin only named — and one key is written
        /// twice, which gettext answers with the first of them.</summary>
        private const string English = """
            msgid ""
            msgstr ""
            "Content-Type: text/plain; charset=UTF-8\n"
            "Language: en\n"

            msgid "Npc_Ronald"
            msgstr "Ronald"

            msgid "Npc_Ronald_Description"
            msgstr "The smith."

            msgid "Npc_Twin"
            msgstr "The twin."

            msgid "Npc_Ronald"
            msgstr "Ronald again."
            """;

        /// <summary>Russian: everything English has but the description, which is the one translation still
        /// to be written.</summary>
        private const string Russian = """
            msgid ""
            msgstr ""
            "Content-Type: text/plain; charset=UTF-8\n"
            "Language: ru\n"

            msgid "Npc_Ronald"
            msgstr "Роналд"

            msgid "Npc_Twin"
            msgstr "Близнец"
            """;

        private string _root = string.Empty;

        [TestInitialize]
        public void CreateTheRoot()
        {
            _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_root);
        }

        [TestCleanup]
        public void DeleteTheRoot()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }

        /// <summary>An id no record of the run answers is named where it stands. The ally beside it, naming
        /// a record the run does hold, has to stay unremarked — a check that reported both would be
        /// reporting that a reference exists.</summary>
        [TestMethod]
        public void AnIdNothingAnswers_IsFoundAndTheOneThatAnswersIsNot()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            CollectionAssert.AreEqual(
                new[] { GhostId },
                Words(found, CatalogFindingKind.UnknownReference),
                Lines(found));
        }

        /// <summary>A catalog nobody described is named once, whatever the word pointing there is: what a
        /// field pointing into it holds cannot be judged at all, and calling every id there broken would
        /// bury the ones that are.</summary>
        [TestMethod]
        public void ACatalogNobodyDescribed_IsNamedOnceAndItsIdsAreNotCalledBroken()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            CollectionAssert.AreEqual(
                new[] { PlaceCatalog },
                Words(found, CatalogFindingKind.UndescribedTarget),
                Lines(found));

            Assert.IsFalse(
                found.Any(finding => finding.Kind == CatalogFindingKind.UnknownReference && finding.Named == "Place_Forge"),
                $"an id pointing into an undescribed catalog was called broken: {Lines(found)}");
        }

        /// <summary>Two records under one name: whichever the reader keeps, the other is out of reach. The
        /// address is the REPEAT's — the record listed first is written where it belongs, and what the
        /// author is looking for is the one too many.</summary>
        [TestMethod]
        public void TwoRecordsWrittenUnderOneId_AreFoundAtTheRepeat()
        {
            CatalogFinding twice = Single(Checked(), CatalogFindingKind.DuplicateId);

            Assert.AreEqual(TwinId, twice.Record);
            Assert.AreEqual(NpcCatalog, twice.Catalog);
            Assert.AreEqual($"{NpcFile}/{NpcsKey}/2", twice.Where);
        }

        /// <summary>A key the catalog words its records under that the reference locale has nothing for,
        /// and one another locale has nothing for. Two facts and not one: the first reaches every player as
        /// the id itself, the second only the players reading that locale.</summary>
        [TestMethod]
        public void AKeyMissingFromTheReferenceLocale_AndOneMissingFromAnother_AreToldApart()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            CollectionAssert.AreEqual(
                new[] { TwinId + DescriptionSuffix },
                Words(found, CatalogFindingKind.MissingText),
                Lines(found));

            CollectionAssert.AreEqual(
                new[] { RonaldId + DescriptionSuffix },
                Words(found, CatalogFindingKind.UntranslatedText),
                Lines(found));
        }

        /// <summary>A key the catalog offers a place for and does not owe is not a key the data is missing.
        /// Nobody wrote the standing card of either npc, and the run says nothing about either — a box in
        /// the tool is a key that MAY be there, and only what the game reads for every record is owed.</summary>
        [TestMethod]
        public void AKeyTheCatalogOffersAndDoesNotOwe_IsNotReportedMissing()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            Assert.IsFalse(
                found.Any(finding => finding.Named.EndsWith(TooltipSuffix, StringComparison.Ordinal)),
                $"a key the catalog only offers was owed all the same: {Lines(found)}");
        }

        /// <summary>The same key where the catalog DOES owe it: every record is held to it then, which is
        /// what makes the list of required suffixes the whole of the rule.</summary>
        [TestMethod]
        public void TheSameKeyWhereTheCatalogOwesIt_IsReportedForEveryRecord()
        {
            IReadOnlyList<CatalogFinding> found = Checked(required: [string.Empty, DescriptionSuffix, TooltipSuffix]);

            CollectionAssert.AreEqual(
                new[] { RonaldId + TooltipSuffix, TwinId + DescriptionSuffix, TwinId + TooltipSuffix },
                Words(found, CatalogFindingKind.MissingText),
                Lines(found));
        }

        /// <summary>Two catalogs word a record under one key and only one of them owes it. The run says the
        /// key once, and the saying belongs to the catalog that OWES it whichever of the two was walked
        /// first: a catalog merely offering the key would otherwise spend the one saying there is and leave
        /// the owed key unsaid for good.</summary>
        [TestMethod]
        public void AKeyOfferedByOneCatalogAndOwedByAnother_IsSaidForTheOneThatOwesIt()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            CatalogFinding[] missing =
            [
                .. found.Where(finding => finding.Kind == CatalogFindingKind.MissingText
                                          && finding.Named == TwinId + DescriptionSuffix)
            ];

            Assert.AreEqual(1, missing.Length, $"another number of sayings of one key than one: {Lines(found)}");
            Assert.AreEqual(NpcCatalog, missing[0].Catalog,
                $"the key was said for a catalog that only offers it: {Lines(found)}");
        }

        /// <summary>A catalog that says nothing about what it owes owes everything it declares: leaving a
        /// key optional is the decision that has to be written down, and a describer forgetting to say so
        /// must not quietly excuse the wording of its whole catalog.</summary>
        [TestMethod]
        public void ACatalogSayingNothingAboutWhatItOwes_OwesEveryKeyItDeclares()
        {
            IReadOnlyList<CatalogFinding> found = Checked(saysWhatItOwes: false);

            CollectionAssert.AreEqual(
                new[] { RonaldId + TooltipSuffix, TwinId + DescriptionSuffix, TwinId + TooltipSuffix },
                Words(found, CatalogFindingKind.MissingText),
                Lines(found));
        }

        /// <summary>A suffix owed and never declared is a key no record is worded under. Refused where the
        /// schema is built, because that is where a misspelling of one of the declared suffixes turns into
        /// a key silently made optional: the catalog would go on being read, owing one key fewer than its
        /// describer says it does.</summary>
        [TestMethod]
        public void ASuffixOwedAndNeverDeclared_IsRefusedBySchema()
        {
            Assert.ThrowsException<ArgumentException>(
                () => Worded([string.Empty, DescriptionSuffix, TooltipSuffix], [string.Empty, MisspeltSuffix]),
                "a catalog was allowed to owe a suffix it words nothing under");
        }

        /// <summary>One catalog's schema as a describer writes it: the keys it words its records under, and
        /// which of them it owes in every locale.</summary>
        private static CatalogSchema Worded(string[] declared, string[] owed) =>
            new CatalogSchema(
                RootShape.ArrayUnderKey,
                [CatalogFixture.Section(NpcsKey, CatalogFixture.Record(IdField, CatalogFixture.Field(IdField, FieldKind.String)))],
                declared,
                new SingleFilePlacement { FileName = NpcCatalog })
            {
                RequiredSuffixes = owed
            };

        /// <summary>A locale writing one key twice. gettext keeps the first, so everything under the second
        /// is out of the game with nothing said about it.</summary>
        [TestMethod]
        public void ALocaleWritingOneKeyTwice_IsFound()
        {
            CatalogFinding twice = Single(Checked(), CatalogFindingKind.DuplicateKey);

            Assert.AreEqual(RonaldId, twice.Named);
            StringAssert.StartsWith(twice.Where, LocalizedTexts.ReferenceLocale, StringComparison.Ordinal);
        }

        /// <summary>A word outside the members offered, in each of the three places a word can stand: a
        /// field of its own, an element of a list and the key of a map. The three written right beside
        /// them stay unremarked — one of them in the wrong case, which the readers take.</summary>
        [TestMethod]
        public void AWordOutsideTheMembersOffered_IsFoundWhereverItStands()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            string[] places =
            [
                .. found.Where(finding => finding.Kind == CatalogFindingKind.UnknownChoice).Select(finding => finding.Where)
            ];

            CollectionAssert.AreEqual(
                new[]
                {
                    $"{NpcFile}/{NpcsKey}/1/{StanceField}",
                    $"{NpcFile}/{NpcsKey}/1/{StancesField}/0",
                    $"{NpcFile}/{NpcsKey}/1/{ParametersField}/Mood"
                },
                places,
                Lines(found));
        }

        /// <summary>A pair of bounds written as a plain number. The one beside it, written as an object with
        /// both ends, has to stay unremarked.</summary>
        [TestMethod]
        public void ARangeWrittenAsANumber_IsFoundAndAPairOfBoundsIsNot()
        {
            CatalogFinding malformed = Single(Checked(), CatalogFindingKind.MalformedRange);

            Assert.AreEqual(NpcCatalog, malformed.Catalog);
            Assert.AreEqual(TwinId, malformed.Record);
            StringAssert.EndsWith(malformed.Where, "/band", StringComparison.Ordinal);
        }

        /// <summary>A record whose shapes are told apart by the presence of a key, wearing both of them and
        /// wearing neither. Both are refused by the reader and neither can be guessed at, so both are said;
        /// the position beside them, wearing one, is not.</summary>
        [TestMethod]
        public void ARecordWearingBothShapesOrNeither_IsFound()
        {
            string[] places =
            [
                .. Checked()
                    .Where(finding => finding.Kind == CatalogFindingKind.AmbiguousShape)
                    .Select(finding => finding.Where)
            ];

            CollectionAssert.AreEqual(
                new[]
                {
                    $"{TableFile}/{TablesKey}/0/{ItemsField}/1",
                    $"{TableFile}/{TablesKey}/0/{ItemsField}/2",
                    $"{TableFile}/{TablesKey}/0/{ItemsField}/3"
                },
                places);
        }

        /// <summary>A shape whose key is written as a flag turned OFF is a shape the record does not wear:
        /// the whole content of the key is a claim, and written that way it says the record does not make
        /// it. So the position writing it reads exactly as the one writing no key at all.</summary>
        [TestMethod]
        public void AShapeKeyWrittenAsAFlagTurnedOff_IsNoShapeWorn()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            CatalogFinding off = At(found, $"{TableFile}/{TablesKey}/0/{ItemsField}/3");
            CatalogFinding none = At(found, $"{TableFile}/{TablesKey}/0/{ItemsField}/2");

            Assert.AreEqual(none.Message, off.Message,
                $"a key written false was read as a shape worn: {Lines(found)}");
        }

        /// <summary>Where the reader takes the shapes in order, a record wearing several is answered by the
        /// first of them: the host says so, and the one wearing none is still nothing to read.</summary>
        [TestMethod]
        public void WhereTheShapesAreReadInOrder_OnlyTheRecordWearingNoneIsSaid()
        {
            IReadOnlyList<CatalogFinding> found = Checked(readsInOrder: _ => true);

            CollectionAssert.AreEqual(
                new[]
                {
                    $"{TableFile}/{TablesKey}/0/{ItemsField}/2",
                    $"{TableFile}/{TablesKey}/0/{ItemsField}/3"
                },
                found.Where(finding => finding.Kind == CatalogFindingKind.AmbiguousShape).Select(finding => finding.Where).ToArray(),
                Lines(found));
        }

        /// <summary>What the host says is not a reference is not read as one, with everything under it: a
        /// record the walk steps over writes no id, and the record beside it still does.</summary>
        [TestMethod]
        public void ARecordTheHostStepsOver_WritesNoIdAtAll()
        {
            IReadOnlyList<CatalogFinding> found = Checked(stepOver: holder => holder.Value<string>(IdField) == TwinId);

            Assert.AreEqual(0, found.Count(finding => finding.Kind == CatalogFindingKind.UnknownReference),
                $"a record the host stepped over was read for ids all the same: {Lines(found)}");
        }

        /// <summary>The boundaries a report must not swallow. A run whose locales could not be read holds
        /// no wording against anything rather than reporting every key of every catalog missing; a catalog
        /// that words nothing is passed over; and a document that is one record answers to no name, so
        /// nothing may be reported as a name written twice.</summary>
        [TestMethod]
        public void ARunWithNoLocales_HoldsNoWordingAndSaysNothingAboutIt()
        {
            IReadOnlyList<CatalogFinding> found = Checked(withTexts: false);

            Assert.AreEqual(0, found.Count(finding => finding.Kind is CatalogFindingKind.MissingText
                                                      or CatalogFindingKind.UntranslatedText
                                                      or CatalogFindingKind.DuplicateKey),
                $"a run that read no locale held the wording against the records all the same: {Lines(found)}");

            Assert.AreNotEqual(0, found.Count, "a run that read no locale found nothing at all — the case proves nothing");
        }

        /// <summary>A catalog written as one record naming nothing: it has no id to be found by, so it
        /// words no key and cannot be written twice.</summary>
        [TestMethod]
        public void ADocumentThatIsOneNamelessRecord_IsPassedOver()
        {
            IReadOnlyList<CatalogFinding> found = Checked();

            Assert.AreEqual(0, found.Count(finding => finding.Catalog == SettingsCatalog),
                $"a settings document was held to rules about records with names: {Lines(found)}");
        }

        /// <summary>Findings arrive in one order every run — catalog, then file, then the address inside it
        /// — so a run can be pinned and two runs compared. Asked by running the checks twice over the same
        /// bench: nothing in the answer may depend on the order a dictionary happened to hand things back
        /// in.</summary>
        [TestMethod]
        public void TheOrderOfTheFindings_IsTheSameEveryRun()
        {
            CollectionAssert.AreEqual(Places(Checked()), Places(Checked()));

            IReadOnlyList<CatalogFinding> found = Checked();
            string[] catalogs = [.. found.Select(finding => finding.Catalog)];

            Assert.IsTrue(
                Array.LastIndexOf(catalogs, NpcCatalog) < Array.IndexOf(catalogs, TableCatalog),
                $"the catalogs were not walked in the order the run opened them:{Environment.NewLine}{Lines(found)}");
        }

        // ── the bench ──────────────────────────────────────────────────────────────────────────────

        private IReadOnlyList<CatalogFinding> Checked(
            bool withTexts = true,
            Func<JObject, bool>? stepOver = null,
            Func<RecordSchema, bool>? readsInOrder = null,
            string[]? required = null,
            bool saysWhatItOwes = true)
        {
            WriteAll(withTexts);

            var workspace = CatalogWorkspace.Load(_root, Descriptors(required, saysWhatItOwes));

            return CatalogChecks.Run(
                workspace,
                new ReferenceIndex(workspace),
                withTexts ? LocalizedTexts.Load(Path.Combine(_root, LocalizationFolder)) : null,
                stepOver,
                readsInOrder);
        }

        private void WriteAll(bool withTexts)
        {
            CatalogFixture.Write(_root, EchoCatalog, EchoFile, Echoes);
            CatalogFixture.Write(_root, NpcCatalog, NpcFile, Npcs);
            CatalogFixture.Write(_root, TableCatalog, TableFile, Tables);
            CatalogFixture.Write(_root, SettingsCatalog, SettingsFile, Settings);

            if (!withTexts) return;

            CatalogFixture.Write(_root, LocalizationFolder, LocalizedTexts.ReferenceLocale + PoCatalogSet.FileExtension, English);
            CatalogFixture.Write(_root, LocalizationFolder, LocalizedTexts.AuthoringLocale + PoCatalogSet.FileExtension, Russian);
        }

        /// <summary>The bench: a catalog whose records are named and worded, one whose positions wear one
        /// of two shapes, and one that is a single nameless document. The npcs are worded under three
        /// suffixes and owe two of them, unless the case asks for another reading — or asks the catalog to
        /// say nothing about what it owes, which is the schema's own reading and not a list written here.</summary>
        private static IReadOnlyList<ICatalogDescriptor> Descriptors(string[]? required, bool saysWhatItOwes) =>
        [
            CatalogFixture.Descriptor(
                EchoCatalog,
                RootShape.ArrayUnderKey,
                [CatalogFixture.Section(EchoesKey, CatalogFixture.Record(IdField, CatalogFixture.Field(IdField, FieldKind.String)))],
                localizedSuffixes: [string.Empty, DescriptionSuffix],
                requiredSuffixes: [string.Empty]),

            CatalogFixture.Descriptor(
                NpcCatalog,
                RootShape.ArrayUnderKey,
                [
                    CatalogFixture.Section(NpcsKey, CatalogFixture.Record(
                        IdField,
                        CatalogFixture.Field(IdField, FieldKind.String),
                        Reference(AllyField, NpcCatalog),
                        Reference(HomeField, PlaceCatalog),
                        Range("band"),
                        Choice(StanceField),
                        new FieldSchema { JsonName = StancesField, Kind = FieldKind.Array, Item = Choice(FieldSchema.Unnamed) },
                        new FieldSchema
                        {
                            JsonName = ParametersField,
                            Kind = FieldKind.Dictionary,
                            Key = new FieldSchema { JsonName = FieldSchema.Unnamed, Kind = FieldKind.Enum, EnumValues = s_parameters },
                            Item = CatalogFixture.Field(FieldSchema.Unnamed, FieldKind.Number)
                        }))
                ],
                localizedSuffixes: [string.Empty, DescriptionSuffix, TooltipSuffix],
                requiredSuffixes: saysWhatItOwes ? required ?? [string.Empty, DescriptionSuffix] : null),

            CatalogFixture.Descriptor(
                TableCatalog,
                RootShape.ArrayUnderKey,
                [
                    CatalogFixture.Section(TablesKey, CatalogFixture.Record(
                        "key",
                        CatalogFixture.Field("key", FieldKind.String),
                        new FieldSchema { JsonName = ItemsField, Kind = FieldKind.Array, Item = Position() }))
                ]),

            CatalogFixture.Descriptor(
                SettingsCatalog,
                RootShape.Single,
                [CatalogFixture.Section(string.Empty, CatalogFixture.Record(null, CatalogFixture.Field("hoursPerDay", FieldKind.Integer)))])
        ];

        /// <summary>The members a stance is written from, and the ones a parameter map is keyed by.</summary>
        private static readonly string[] s_stances = ["Dexterity", "Strength"];

        private static readonly string[] s_parameters = ["Health", "Mana"];

        /// <summary>A field drawn from a named set of members — the one shape a word outside them is
        /// judged by, wherever it stands.</summary>
        private static FieldSchema Choice(string jsonName) =>
            new() { JsonName = jsonName, Kind = FieldKind.Enum, EnumValues = s_stances };

        private static FieldSchema Reference(string jsonName, string catalog) => new()
        {
            JsonName = jsonName,
            Kind = FieldKind.Reference,
            RefTargets = [ReferenceTarget.Whole(catalog)]
        };

        /// <summary>A field the schema reads as a pair of bounds: two numbers and nothing else.</summary>
        private static FieldSchema Range(string jsonName) => new()
        {
            JsonName = jsonName,
            Kind = FieldKind.Object,
            Record = new RecordSchema
            {
                TypeName = "Band",
                Fields = [CatalogFixture.Field(MinBound, FieldKind.Number), CatalogFixture.Field(MaxBound, FieldKind.Number)]
            }
        };

        /// <summary>One seat of a table: it names a drop or a group of them, told apart by which key is
        /// there, and carries a price whichever it names.</summary>
        private static FieldSchema Position() => new()
        {
            JsonName = FieldSchema.Unnamed,
            Kind = FieldKind.Object,
            Record = new RecordSchema
            {
                TypeName = "Position",
                Fields = [CatalogFixture.Field(PriceField, FieldKind.Number)],
                Variants = new VariantSet(
                [
                    new VariantSchema
                    {
                        DiscriminatorValue = IdField,
                        Record = new RecordSchema { TypeName = "ById", Fields = [Reference(IdField, NpcCatalog)] }
                    },
                    new VariantSchema
                    {
                        DiscriminatorValue = GroupField,
                        Record = new RecordSchema { TypeName = "ByGroup", Fields = [CatalogFixture.Field(GroupField, FieldKind.String)] }
                    }
                ])
            }
        };

        private static string[] Words(IReadOnlyList<CatalogFinding> found, CatalogFindingKind kind) =>
            [.. found.Where(finding => finding.Kind == kind).Select(finding => finding.Named)];

        private static string[] Places(IReadOnlyList<CatalogFinding> found) =>
            [.. found.Select(finding => $"{finding.Kind} {finding.Catalog} {finding.Record} {finding.Named} {finding.Where}")];

        /// <summary>The one finding standing at an address, which is how two records written differently
        /// are told apart without pinning the wording of either message.</summary>
        private static CatalogFinding At(IReadOnlyList<CatalogFinding> found, string where)
        {
            CatalogFinding[] said = [.. found.Where(finding => finding.Where == where)];

            Assert.AreEqual(1, said.Length, $"another number of findings at '{where}' than one: {Lines(found)}");

            return said[0];
        }

        private static CatalogFinding Single(IReadOnlyList<CatalogFinding> found, CatalogFindingKind kind)
        {
            CatalogFinding[] said = [.. found.Where(finding => finding.Kind == kind)];

            Assert.AreEqual(1, said.Length, $"another number of {kind} findings than one: {Lines(found)}");

            return said[0];
        }

        private static string Lines(IReadOnlyList<CatalogFinding> found) =>
            found.Count == 0
                ? "the run found nothing at all"
                : string.Join(Environment.NewLine, Places(found));
    }
}
