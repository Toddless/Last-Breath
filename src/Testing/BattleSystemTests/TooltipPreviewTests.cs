namespace LastBreathTest.BattleSystemTests
{
    using System.IO;
    using System.Linq;
    using Core.Data.GameData;
    using Core.Enums;
    using LastBreath.Descriptors;
    using LastBreath.Descriptors.Preview;
    using Newtonsoft.Json.Linq;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Localization;

    /// <summary>
    /// The reading an authoring tool shows beside a record: the game's own minter, line builders and
    /// formatters over the documents and the .po files the tool has OPEN. What is checked here is that
    /// the reading is the GAME's — the wording comes out of the shipped locales, the numbers out of the
    /// shipped catalogs, and a figure retyped in either reaches the next reading.
    /// </summary>
    [TestClass]
    public sealed class TooltipPreviewTests
    {
        /// <summary>The folder the locales sit in beside the catalogs.</summary>
        private const string Localization = "Localization";

        /// <summary>A template with a typed base stat, a pool of its own and no authored rarity — the
        /// plain case a preview is opened on most.</summary>
        private const string Helmet = "Helmet_Steel";

        private const string Armor = "Armor";

        /// <summary>An augment stating no figures of its own: every number on its line belongs to the
        /// canon of the effect it lays, which is the road a tool holding no battle module has to reach.</summary>
        private const string Augment = "Augment_Attacks_Reduce_Armor";

        /// <summary>The share the canon balances that augment's effect at, as its line prints it. Named
        /// here because it is the whole point of the walk: the record states no figure, so a card
        /// carrying this one is a card that reached the balance file.</summary>
        private const string LaidFigure = "15%";

        /// <summary>An effect the wording gives a card of its own — the key a keyword link opens.</summary>
        private const string Charge = "Effect_Charge";

        /// <summary>A plain item — no lines, no stats, only what the bag would call it.</summary>
        private const string Recipe = "Recipe_Creators_Ring";

        /// <summary>An id no locale writes and no catalog declares.</summary>
        private const string Unwritten = "Effect_Nobody_Wrote_This";

        /// <summary>Json names of the base stat a mutation walk retypes.</summary>
        private const string BaseStats = "baseStats";

        private const string Value = "value";

        /// <summary>A number no shipped template carries, so finding it on a card can only mean the
        /// reading came from the document the test wrote it into.</summary>
        private const int Retyped = 4242;

        [TestMethod]
        public void AnEquipTemplate_ReadsItsNameItsBaseStatAndTheLinesItsRarityRolls()
        {
            LocalizedTexts texts = Texts();
            TooltipPreview preview = Preview(Workspace(), texts);

            PreviewText card = preview.Describe(DataCatalog.EquipItems, Helmet, Rarity.Legendary);

            Assert.AreEqual(texts.Read(PreviewWording.Fallback, Helmet), card.Title,
                "the title is the name the shipped wording writes for the template");
            Assert.IsTrue(card.Lines.Any(line => line.StartsWith(Armor, System.StringComparison.Ordinal) && line.Any(char.IsDigit)),
                $"no base stat row carried the piece's armour:\n  {string.Join("\n  ", card.Lines)}");
            Assert.IsTrue(card.Lines.Count(line => line.Any(char.IsDigit)) > 1,
                $"a legendary rolls lines, and only one row of this card carries a number:\n  {string.Join("\n  ", card.Lines)}");
        }

        /// <summary>The same template in the other locale. Held against the .po itself rather than against
        /// a Russian word written into the test: the wording is edited daily and what is being checked is
        /// that the reading comes out of the file, not that the file says one particular thing.</summary>
        [TestMethod]
        public void TheSameTemplate_ReadsInWhicheverLocaleIsAskedFor()
        {
            LocalizedTexts texts = Texts();
            TooltipPreview preview = Preview(Workspace(), texts);

            PreviewText english = preview.Describe(DataCatalog.EquipItems, Helmet, Rarity.Rare);

            preview.Wording.Locale = LocalizedTexts.AuthoringLocale;

            PreviewText russian = preview.Describe(DataCatalog.EquipItems, Helmet, Rarity.Rare);

            Assert.AreEqual(texts.Read(LocalizedTexts.AuthoringLocale, Helmet), russian.Title);
            Assert.AreNotEqual(english.Title, russian.Title, "the two locales write this name differently");
            Assert.IsTrue(russian.Lines.Any(line => line.StartsWith(texts.Read(LocalizedTexts.AuthoringLocale, Armor)!, System.StringComparison.Ordinal)),
                $"the base stat row kept the English parameter name:\n  {string.Join("\n  ", russian.Lines)}");
        }

        /// <summary>An augment that restates none of its effect's figures still prints them: the numbers
        /// come from the canonical row, which a tool reads without the registry that builds effects.</summary>
        [TestMethod]
        public void AnAugment_PrintsTheFiguresOfTheEffectItLays()
        {
            TooltipPreview preview = Preview(Workspace(), Texts());

            PreviewText card = preview.Describe(DataCatalog.Abilities, Augment, Rarity.Legendary);
            string line = string.Join(" ", card.Lines);

            Assert.IsFalse(line.Contains('{'), $"the augment's line kept a placeholder: {line}");
            Assert.IsTrue(line.Contains(LaidFigure, System.StringComparison.Ordinal),
                $"the figure the canon balances the laid effect at is not on the line: {line}");
        }

        /// <summary>An effect reads its rule and, under it, the standing card a keyword link opens.</summary>
        [TestMethod]
        public void AnEffect_ReadsItsKeywordCardBesideItsRule()
        {
            LocalizedTexts texts = Texts();
            TooltipPreview preview = Preview(Workspace(), texts);

            PreviewText card = preview.Describe(DataCatalog.Effects, Charge, Rarity.Common);

            Assert.AreEqual(texts.Read(PreviewWording.Fallback, Charge), card.Title);
            Assert.IsTrue(card.Lines.Contains(texts.Read(PreviewWording.Fallback, Charge + "_Tooltip")),
                $"the keyword card of the effect is not among its lines:\n  {string.Join("\n  ", card.Lines)}");
        }

        /// <summary>A key nobody wrote stands for itself, and the row it belongs to is named as having no
        /// canonical figures. The tool shows the miss rather than inventing a word for it — a preview that
        /// filled the gap would hide the one thing this panel is watched for.</summary>
        [TestMethod]
        public void AnIdNoLocaleWrites_StandsForItself()
        {
            TooltipPreview preview = Preview(Workspace(), Texts());

            PreviewText card = preview.Describe(DataCatalog.Effects, Unwritten, Rarity.Common);

            Assert.AreEqual(Unwritten, card.Title);
            Assert.IsTrue(card.Lines.Any(line => line.Contains(Unwritten, System.StringComparison.Ordinal)),
                $"nothing on the card names the key that is missing:\n  {string.Join("\n  ", card.Lines)}");
        }

        /// <summary>A recipe is its name and its subtitle and nothing else: there are no lines to roll on
        /// a plain item, and the reading is what the bag would show of it.</summary>
        [TestMethod]
        public void ARecipe_ReadsTheNameItsWordingWrites()
        {
            LocalizedTexts texts = Texts();

            PreviewText card = Preview(Workspace(), texts).Describe(DataCatalog.Recipes, Recipe, Rarity.Common);

            Assert.AreEqual(texts.Read(PreviewWording.Fallback, Recipe), card.Title);
            Assert.IsTrue(card.Lines.Count > 0, "a recipe reads at least the rarity it is written at");
        }

        /// <summary>A record too unfinished to be read is said so, not thrown over: an id half typed is
        /// the state an author spends most of his time in.</summary>
        [TestMethod]
        public void ARecordWithNoIdYet_IsSaidSoInsteadOfBuilt()
        {
            PreviewText card = Preview(Workspace(), Texts()).Describe(DataCatalog.EquipItems, string.Empty, Rarity.Rare);

            Assert.AreEqual(string.Empty, card.Title);
            Assert.AreEqual(1, card.Lines.Count);
        }

        /// <summary>A number retyped in the document reaches the next reading — the documents the readers
        /// are handed are the open ones, and nothing is read off disk after the tool opened.</summary>
        [TestMethod]
        public void ANumberRetypedInTheDocument_ReachesTheNextReading()
        {
            CatalogWorkspace workspace = Workspace();
            LocalizedTexts texts = Texts();

            PreviewText before = Preview(workspace, texts).Describe(DataCatalog.EquipItems, Helmet, Rarity.Common);

            Retype(workspace, Helmet, "min", Retyped);
            Retype(workspace, Helmet, "max", Retyped);

            PreviewText after = Preview(workspace, texts).Describe(DataCatalog.EquipItems, Helmet, Rarity.Common);

            Assert.IsFalse(before.Lines.Any(line => line.Contains(Retyped.ToString(System.Globalization.CultureInfo.InvariantCulture), System.StringComparison.Ordinal)),
                "the shipped template already reads 4242, so this walk proves nothing");
            Assert.IsTrue(after.Lines.Any(line => line.Contains(Retyped.ToString(System.Globalization.CultureInfo.InvariantCulture), System.StringComparison.Ordinal)),
                $"the retyped armour is not on the card:\n  {string.Join("\n  ", after.Lines)}");
        }

        /// <summary>A wording retyped in the locale reaches the reading too, and without reloading a
        /// thing: the locales are read live, which is what the block of text beside a record edits.</summary>
        [TestMethod]
        public void AWordingRetypedInTheLocale_ReachesTheNextReading()
        {
            LocalizedTexts texts = Texts();
            TooltipPreview preview = Preview(Workspace(), texts);

            PreviewText before = preview.Describe(DataCatalog.EquipItems, Helmet, Rarity.Common);

            Assert.IsTrue(texts.Write(PreviewWording.Fallback, Helmet, "Retyped Helmet"), "the locale refused the edit");

            PreviewText after = preview.Describe(DataCatalog.EquipItems, Helmet, Rarity.Common);

            Assert.AreNotEqual(before.Title, after.Title);
            Assert.AreEqual("Retyped Helmet", after.Title);
        }

        /// <summary>Writes one end of a template's first base stat into the OPEN document, the way the
        /// inspector's own box does — on the document and never on disk.</summary>
        private static void Retype(CatalogWorkspace workspace, string recordId, string end, int value)
        {
            CatalogRecord record = workspace.Catalogs
                .Single(view => view.Catalog == DataCatalog.EquipItems).Records
                .Single(entry => entry.CurrentId == recordId);

            JsonPointer at = record.Pointer.Append(BaseStats).Append(0).Append(Value).Append(end);

            Assert.IsTrue(record.File.Document.SetValue(at, new JValue(value)), $"the document refused to take '{at}'");
        }

        private static TooltipPreview Preview(CatalogWorkspace workspace, LocalizedTexts texts) =>
            TooltipPreview.Load(workspace, texts);

        private static CatalogWorkspace Workspace() =>
            CatalogWorkspace.Load(SharedData.Root(), CatalogDescriptors.All);

        private static LocalizedTexts Texts() =>
            LocalizedTexts.Load(Path.Combine(SharedData.Root(), Localization));
    }
}
