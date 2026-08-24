namespace LastBreathTest.BattleSystemTests
{
    using System.Text;
    using Battle.Source.RequestHandlers;
    using Battle.Source.UIElements.PassiveWheel;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus.Requests;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Core.Views;
    using Core.Views.UI;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The localization audit: keys required by SharedData ids vs en.po, and en.po vs ru.po.
    /// Reports gaps to the test output (Inconclusive, never red); the only hard failure is a
    /// duplicate msgid — that corrupts the .po.
    /// </summary>
    [TestClass]
    public class LocalizationAuditTests
    {
        /// <summary>The minter needs a die; nothing here reads what it rolls.</summary>
        private const int Seed = 11;

        private static string SrcRoot
        {
            get
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "SharedData")))
                    dir = dir.Parent;
                Assert.IsNotNull(dir, "SharedData not found above test bin directory");
                return dir.FullName;
            }
        }

        private static string Shared => Path.Combine(SrcRoot, "SharedData");

        [TestMethod]
        public void PoFilesHaveNoDuplicateKeys()
        {
            foreach (string po in new[] { "en.po", "ru.po" })
            {
                var duplicates = ReadMsgIds(po).GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                Assert.AreEqual(0, duplicates.Count, $"{po} duplicate msgids: {string.Join(", ", duplicates)}");
            }
        }

        [TestMethod]
        public void ReportMissingKeys()
        {
            var en = ReadMsgIds("en.po").ToHashSet();
            var ru = ReadMsgIds("ru.po").ToHashSet();
            var report = new StringBuilder();
            int missingTotal = 0;

            foreach ((string domain, List<string> ids, bool needsDescription) in CollectDataIds())
            {
                var missingNames = ids.Where(id => !en.Contains(id)).ToList();
                var missingDescriptions = needsDescription ? ids.Where(id => !en.Contains(id + "_Description")).ToList() : [];
                missingTotal += missingNames.Count + missingDescriptions.Count;

                if (missingNames.Count > 0)
                    report.AppendLine($"[{domain}] нет имён в en.po ({missingNames.Count}):\n  {string.Join("\n  ", missingNames)}");
                if (missingDescriptions.Count > 0)
                    report.AppendLine($"[{domain}] нет описаний в en.po ({missingDescriptions.Count}):\n  {string.Join("\n  ", missingDescriptions.Select(id => id + "_Description"))}");
            }

            var missingInRu = en.Where(id => !ru.Contains(id)).OrderBy(id => id).ToList();
            if (missingInRu.Count > 0)
                report.AppendLine($"[ru.po] ключей нет вовсе ({missingInRu.Count}):\n  {string.Join("\n  ", missingInRu)}");

            Console.WriteLine(report.Length == 0 ? "Локализация полна." : report.ToString());
            if (missingTotal + missingInRu.Count > 0)
                Assert.Inconclusive($"en.po: не хватает {missingTotal} ключей из данных; ru.po: отстаёт на {missingInRu.Count} ключей. Полный список — в output теста.");
        }

        /// <summary>
        /// The augment tooltip's tier line, both halves of it. A key with no wording prints itself, and a
        /// wording whose placeholder nobody fills prints the placeholder — the tier reaches the player only
        /// when the catalog words the key under the name <see cref="Battle.Source.UIElements.AugmentTrayTile"/>
        /// puts in its values.
        /// </summary>
        [TestMethod]
        public void AugmentTierLineIsWordedAndFilledByTheTilePlaceholder()
        {
            const string key = "UI_Augment_Tier";
            const string tilePlaceholder = "Value";

            string? template = ReadEntries("en.po").GetValueOrDefault(key);
            Assert.IsFalse(string.IsNullOrEmpty(template), $"en.po words no '{key}' — the tooltip shows the raw key");

            var provider = new FakeLocalizationProvider();
            string line = new TextTemplateEngine(provider)
                .Render(template!, new Dictionary<string, object?> { [tilePlaceholder] = 2 }, TextFormat.Plain);

            StringAssert.Contains(line, "2", $"'{key}' never places the tier — it names no '{{{tilePlaceholder}}}'");
            Assert.IsFalse(line.Contains('{'), $"'{key}' keeps a placeholder the tile does not fill: {line}");
        }

        /// <summary>The line a socket node of the passive tree prints: which tier of slot it opens and
        /// whose. A key with no wording prints itself, and a wording whose placeholders nobody fills prints
        /// the placeholders, so both names the wheel puts in its values have to be in the catalog's.</summary>
        [TestMethod]
        public void TheSocketNodesOwnerLineIsWordedAndFilledByTheWheelsPlaceholders()
        {
            const string ability = "Ability_Dex";

            string? template = ReadEntries("en.po").GetValueOrDefault(PassiveWheelText.SlotOwner);
            Assert.IsFalse(string.IsNullOrEmpty(template),
                $"en.po words no '{PassiveWheelText.SlotOwner}' — the popup shows the raw key");

            string line = new TextTemplateEngine(new FakeLocalizationProvider()).Render(
                template!,
                new Dictionary<string, object?>
                {
                    [PassiveWheelText.TierValue] = 2,
                    [PassiveWheelText.NameValue] = ability
                },
                TextFormat.Plain);

            StringAssert.Contains(line, "2", $"'{PassiveWheelText.SlotOwner}' never places the tier");
            StringAssert.Contains(line, ability, $"'{PassiveWheelText.SlotOwner}' never names the ability");
            Assert.IsFalse(line.Contains('{'), $"'{PassiveWheelText.SlotOwner}' keeps a placeholder the wheel does not fill: {line}");
        }

        /// <summary>
        /// Every tag of the vocabulary is worded. Tags are printed on the card of an ability and on the
        /// card of an augment alike, and an unworded one reaches the player as the raw key — which is the
        /// vocabulary's internal spelling, in lower case, in the middle of a sentence.
        /// </summary>
        [TestMethod]
        public void EveryCombatTagOfTheVocabularyIsWordedInEnglish()
        {
            Dictionary<string, string> en = ReadEntries("en.po");
            List<string> unworded = AbilityTags.All
                .Select(TagText.KeyOf)
                .Where(key => string.IsNullOrEmpty(en.GetValueOrDefault(key)))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            Assert.AreEqual(0, unworded.Count,
                $"en.po words no name for {unworded.Count} tag(s), so a card prints the raw key: {string.Join(", ", unworded)}");
        }

        /// <summary>
        /// Every tag every augment record of the shipped catalog declares is worded. The guard above
        /// covers the words the CODE knows; this one covers the words the DATA uses — a record's tags are
        /// printed verbatim on its card as the line telling the player where the augment goes, so an
        /// unworded (or differently spelled) one reaches him as a raw key in the middle of that line.
        /// </summary>
        [TestMethod]
        public void EveryTagOfEveryAugmentRecordIsWordedInEnglish()
        {
            Dictionary<string, string> en = ReadEntries("en.po");
            List<string> unworded = ShippedAbilityData.Augments().All
                .SelectMany(record => record.Tags)
                .Distinct(StringComparer.Ordinal)
                .Select(TagText.KeyOf)
                .Where(key => string.IsNullOrEmpty(en.GetValueOrDefault(key)))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            Assert.AreEqual(0, unworded.Count,
                $"en.po words no name for {unworded.Count} tag(s) an augment record declares: {string.Join(", ", unworded)}");
        }

        /// <summary>
        /// Where an augment says it goes, read off the shipped records and the shipped wording. Three
        /// declarations, three sentences: tags for the records seated by a shared tag, the ability's own
        /// name for the ones written for one, and the claim itself for the ones at home everywhere.
        /// </summary>
        [TestMethod]
        public void TheFittingLineOfAnAugmentReadsOffTheShippedCatalogAndWording()
        {
            UseEnglishCatalog();
            IAbilityAugmentCatalog augments = ShippedAbilityData.Augments();

            Assert.AreEqual("Attack, Series", FitLineOf(augments, "Augment_Apply_Buff_Critical_Damage"),
                "an unbound record no longer names the tags that seat it");
            Assert.AreEqual("Cost, Cooldown", FitLineOf(augments, "Augment_Reduce_Cooldown_And_Cost"),
                "the mechanical axes were swallowed — on an augment they are the whole answer");
            Assert.AreEqual("Fits: Critical Calculation", FitLineOf(augments, "Augment_Critical_Calculation_Mythic"),
                "a record written for one ability does not name it");
            Assert.AreEqual("Fits: any ability", FitLineOf(augments, "Augment_Reduce_Cost"),
                "a record at home on every ability does not say so");
        }

        private static string FitLineOf(IAbilityAugmentCatalog augments, string augmentId)
        {
            Core.Data.AbilityData.AbilityAugmentData? record = augments.Find(augmentId);
            Assert.IsNotNull(record, $"the shipped catalog declares no '{augmentId}' to read");
            return AugmentText.FitLine(record.Tags, record.AbilityId, record.FitsAnyAbility);
        }

        /// <summary>
        /// The price and the wait, worded out of the shipped catalog. Both go through one templating, so a
        /// key nobody worded or a placeholder nobody fills is visible here rather than in a screenshot. The
        /// wait also has to COUNT: a cooldown of one is one turn, and a card reading "1 turns" is a card
        /// written by a machine.
        /// </summary>
        [TestMethod]
        public void ThePriceAndTheWaitAreWordedAndTheWaitCountsItsTurns()
        {
            UseEnglishCatalog();

            Assert.AreEqual("Cost: 150 Mana", AbilityText.CostLine(150, Costs.Mana),
                "the price line lost its template, its number or its resource");
            Assert.AreEqual("Cooldown: 3 turns", AbilityText.CooldownLine(3f));
            Assert.AreEqual("Cooldown: 1 turn", AbilityText.CooldownLine(1f),
                "the wait lost its plural form — one turn is not 'turns'");
            Assert.AreEqual(string.Empty, AbilityText.CooldownLine(0f),
                "a cast that makes nobody wait printed a line about waiting");
        }

        /// <summary>
        /// The card of an ability NOBODY owns, read the way the passive wheel reads it: the shipped
        /// catalog, the real handler, the real wording. This is most of what the wheel prints — the player
        /// is looking at nodes he has not bought — and it is built from an instance nobody learned, so a
        /// card that only works for the owned half would leave the whole screen mute.
        /// </summary>
        [TestMethod]
        public async Task TheCardOfAnAbilityNobodyOwnsIsFullyWordedInTheShippedCatalog()
        {
            const string ability = "Ability_Head_Butt";

            UseEnglishCatalog();
            (Battle.Source.Abilities.AbilityProvider abilities, AbilityAugmentCatalog augments) = ShippedAbilityData.Load();
            var handler = new AbilitySocketRowsRequestHandler(
                new AbilitySocketBoard(augments),
                AbilityBookStand.AccessorFor(AbilityBookStand.NewBook()),
                abilities,
                new AugmentItemMinter(augments, new AugmentMinter(augments, new DefaultRandomNumberGenerator(Seed))),
                augments,
                _ => null);

            IReadOnlyList<AbilitySocketRowView> rows = await handler.HandleRequest(
                new GetAbilitySocketRowsRequest(AbilityId: ability, IncludeUnowned: true));

            AbilitySocketRowView row = rows.Single();
            Assert.IsFalse(row.IsOwned, "the book was not empty, so the case proves nothing about an unowned card");

            AbilityCard card = AbilityText.Card(row);
            Assert.AreNotEqual(string.Empty, card.MetaLine, "the card of an unowned ability names no price and no wait");
            Assert.AreNotEqual(string.Empty, card.TagsLine, "the card of an unowned ability says nothing about what it counts as");
            Assert.AreNotEqual(string.Empty, card.Description, "the card of an unowned ability says nothing about what it does");
            Assert.IsFalse(card.Body.Contains('{'), $"the card keeps a placeholder nobody fills: {card.Body}");
        }

        /// <summary>The shipped wording, pinned to the static facade every card reads through.</summary>
        private static void UseEnglishCatalog()
        {
            var catalog = new FakeLocalizationProvider();
            foreach ((string key, string wording) in ReadEntries("en.po")) catalog.Strings[key] = wording;

            Localization.Override(new LocalizationService(catalog,
                new ModifierFormatter(catalog, new ParameterFormatProvider()),
                new ContextModifierFormatter(catalog), []));
        }

        /// <summary>
        /// The keystone that pays for its gift, read the way the wheel reads it — shipped tree, shipped
        /// catalog, no fixture in between. The penalty has to arrive as a sentence a player parses at a
        /// glance and never as a signed multiplier ("-25% more"), which reads like a bonus going the
        /// wrong way.
        /// </summary>
        [TestMethod]
        public void TheKeystonesPenaltyReadsAsASentenceInTheShippedTree()
        {
            const string keystone = "keystone_6";
            const string penalty = "25% less Health Recovery";

            var tree = new PassiveTreeProvider();
            var formats = new ParameterFormatProvider();
            new GameDataService(new FileSystemDataSource(LastBreathTest.SharedData.Root()), [tree, formats]).LoadAll();

            PassiveNode? node = tree.Tree.Find(keystone);
            Assert.IsNotNull(node, $"the shipped tree carries no '{keystone}' to read");

            var catalog = new FakeLocalizationProvider();
            foreach ((string key, string wording) in ReadEntries("en.po")) catalog.Strings[key] = wording;

            string[] lines = [.. PassiveNodeLines.Of(node!, new ModifierFormatter(catalog, formats), null, catalog)
                .Select(line => line.Text)];

            CollectionAssert.Contains(lines, penalty, $"'{keystone}' words its cost as: {string.Join(" | ", lines)}");
        }

        /// <summary>
        /// A composite node read the way the wheel reads it — shipped tree, shipped catalog. Two records
        /// stamped into one group have to arrive as ONE sentence with both numbers in it; two rows saying
        /// half of the node each is what the stamp exists to prevent.
        /// </summary>
        [TestMethod]
        public void ACompositeNodeOfTheShippedTreeReadsAsASingleLine()
        {
            const string composite = "small_mana_mana_recovery_1";
            const string sentence = "+2% increased Mana, +3% increased Mana Recovery";

            var tree = new PassiveTreeProvider();
            var formats = new ParameterFormatProvider();
            new GameDataService(new FileSystemDataSource(LastBreathTest.SharedData.Root()), [tree, formats]).LoadAll();

            PassiveNode? node = tree.Tree.Find(composite);
            Assert.IsNotNull(node, $"the shipped tree carries no '{composite}' to read");

            var catalog = new FakeLocalizationProvider();
            foreach ((string key, string wording) in ReadEntries("en.po")) catalog.Strings[key] = wording;

            string[] lines = [.. PassiveNodeLines.Of(node!, new ModifierFormatter(catalog, formats), null, catalog)
                .Select(line => line.Text)];

            Assert.AreEqual(1, lines.Length, $"'{composite}' still speaks in parts: {string.Join(" | ", lines)}");
            Assert.AreEqual(sentence, lines[0]);
        }

        private static List<(string Domain, List<string> Ids, bool NeedsDescription)> CollectDataIds() =>
        [
            // Modifier and ParameterChange templates localize parameter names by enum member
            ("EntityParameter", [.. Enum.GetNames<Core.Enums.EntityParameter>()], false),
            ("EquipItems", CatalogIds("EquipItems", "items"), true),
            ("Items", CatalogIds("Items", "items"), true),
            ("Recipes", CatalogIds("Recipes", "craftingRecipes"), false),
            ("Resources", [.. CatalogIds("Resources", "upgradeResources"), .. CatalogIds("Resources", "craftingResources")], false),
            ("Abilities", CatalogIds("Abilities", "abilities"), true),
            // Augments are records of their own in the same catalog, not a nested list under an ability
            ("Augments", CatalogIds("Abilities", "augments"), true),
            ("Npc", CatalogIds("Npc", "npcs"), false),
            ("NpcModifiers", NestedIds("NpcModifiers", "mods", "modifiers"), true),
            // A condition is worded under a key derived from its id, not under the id itself
            ("Conditions", CatalogIds("Conditions", "conditions").Select(Core.Localization.ConditionalLineText.ClauseKey).ToList(), false),
        ];

        private static List<string> CatalogIds(string catalog, string arrayProperty) =>
            CatalogRoots(catalog)
                .SelectMany(root => root[arrayProperty] as JArray ?? [])
                .Select(token => (string?)token["id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

        private static List<string> NestedIds(string catalog, string outerProperty, string innerProperty) =>
            CatalogRoots(catalog)
                .SelectMany(root => root[outerProperty] as JArray ?? [])
                .SelectMany(outer => outer[innerProperty] as JArray ?? [])
                .Select(token => (string?)token["id"])
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

        private static IEnumerable<JObject> CatalogRoots(string catalog)
        {
            string path = Path.Combine(Shared, catalog);
            if (!Directory.Exists(path)) yield break;
            foreach (string file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                yield return JObject.Parse(File.ReadAllText(file));
        }

        /// <summary>Key → wording for the one-line entries the catalog is written in; a continued
        /// entry simply comes back with its first line, which is enough to tell a worded key from an
        /// empty one.</summary>
        private static Dictionary<string, string> ReadEntries(string poFileName)
        {
            string[] lines = File.ReadAllLines(Path.Combine(Shared, "Localization", poFileName));
            var entries = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i + 1 < lines.Length; i++)
            {
                if (!Quoted(lines[i], "msgid ", out string id) || id.Length == 0) continue;
                if (Quoted(lines[i + 1], "msgstr ", out string text)) entries[id] = text;
            }

            return entries;
        }

        private static bool Quoted(string line, string prefix, out string value)
        {
            value = string.Empty;
            if (!line.StartsWith(prefix + '"', StringComparison.Ordinal) || !line.EndsWith('"')) return false;

            value = line[(prefix.Length + 1)..^1];
            return true;
        }

        private static List<string> ReadMsgIds(string poFileName)
        {
            string path = Path.Combine(Shared, "Localization", poFileName);
            return File.ReadAllLines(path)
                .Where(line => line.StartsWith("msgid \"", StringComparison.Ordinal))
                .Select(line => line["msgid \"".Length..^1])
                .Where(id => id.Length > 0)
                .ToList();
        }
    }
}
