namespace LastBreathTest.BattleSystemTests
{
    using System.Text;
    using Battle.Source.RequestHandlers;
    using Battle.Source.UIElements.PassiveWheel;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Localization;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Modifiers.Context;
    using Core.PassiveTree;
    using Core.PassiveTree.View;
    using Core.Views;
    using Core.Views.UI;
    using LootGeneration.Internal;
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
        /// The wording twin of <see cref="ContextModifierBindingTests.EveryContextParameter_HasABinding"/>:
        /// that one keeps a knob from reaching no pipeline, this one keeps a knob from reaching the player
        /// as its own internal spelling. A context line is worded through Context_Modifier_&lt;Parameter&gt;
        /// (see <see cref="ContextModifierFormatter"/>), so a knob the catalog never worded prints
        /// "Context_Modifier_BuffEffectivenessScale" on a tree node and on an item card alike.
        /// <para>The _Range twin belongs to every knob that carries a NUMBER, because that is what an
        /// unrolled pool spread renders through; a switch has no spread to word and is asked for the plain
        /// key alone. Which knobs are switches is read off the binding table rather than listed here.</para>
        /// </summary>
        [TestMethod]
        public void EveryContextKnobIsWordedInEnglishAndSoIsItsSpread()
        {
            Dictionary<string, string> en = ReadEntries("en.po");
            var unworded = new List<string>();

            foreach (ContextParameter knob in Enum.GetValues<ContextParameter>())
            {
                if (string.IsNullOrEmpty(en.GetValueOrDefault($"Context_Modifier_{knob}"))) unworded.Add($"Context_Modifier_{knob}");
                if (ContextKnobs.IsFlag(knob)) continue;
                if (string.IsNullOrEmpty(en.GetValueOrDefault($"Context_Modifier_{knob}_Range"))) unworded.Add($"Context_Modifier_{knob}_Range");
            }

            Assert.AreEqual(0, unworded.Count,
                $"en.po words no template for {unworded.Count} context key(s), so the line prints the raw key: {string.Join(", ", unworded.Order(StringComparer.Ordinal))}");
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

        /// <summary>
        /// The keystone wave read the way its popup reads it: the shipped list of ids, the real registry,
        /// the real wording — in BOTH catalogs, because a key worded in English and forgotten in Russian
        /// reaches half the players as its own internal spelling.
        /// <para>Three failures, one guard. A key nobody worded prints itself ("Passive_Skill_Trinity"); a
        /// key worded empty prints nothing at all; and a template naming a value the passive never hands it
        /// keeps the token, so the card reads "60% less" as "{ResistanceCut} less". The last is the one that
        /// only shows up here — it needs the wording and the skill in the same room.</para>
        /// </summary>
        [TestMethod]
        public void EveryKeystoneOfTheWaveIsNamedAndWordedInBothCatalogs()
        {
            var provider = new Battle.Source.PassiveSkillProvider();
            Core.Battle.Skills.PassiveSkillCatalog shipped = ShippedPassiveCatalog();
            var complaints = new List<string>();

            foreach (string po in new[] { "en.po", "ru.po" })
            {
                UseCatalog(po);

                foreach (string id in s_keystoneWave)
                {
                    Core.Battle.Skills.ISkill? skill = provider.CreateSkill(id, KeystoneFields(id, shipped));
                    Assert.IsNotNull(skill, $"the registry no longer builds '{id}' from the fields the catalog declares");

                    string name = skill.DisplayName;
                    string description = skill.Description;

                    if (string.IsNullOrWhiteSpace(name) || name == id)
                        complaints.Add($"{po}: '{id}' has no name — the popup shows the raw key");
                    if (string.IsNullOrWhiteSpace(description) || description == id + LocalizationService.DescriptionSuffix)
                        complaints.Add($"{po}: '{id}' has no description — the popup shows the raw key");
                    else if (description.Contains('{'))
                        complaints.Add($"{po}: '{id}' keeps a placeholder the passive does not fill: {description}");
                }
            }

            UseEnglishCatalog();
            Assert.AreEqual(0, complaints.Count, string.Join("\n", complaints));
        }

        /// <summary>
        /// The sign of a price, pinned on the one keystone the shipped tree already tunes. Every penalty of
        /// the wave is stored as the negative share its modifier is built from (−0.6), and a card rendering
        /// that share into a sentence that already says "less" reads "-60% less Poison Resistance" — a cost
        /// stated twice and pointing both ways. The wording asks for the magnitude instead, and this is what
        /// holds it there.
        /// </summary>
        [TestMethod]
        public void TheKeystonesPriceReachesTheCardAsAMagnitudeAndNotAsASignedShare()
        {
            const float perOvercap = 0.01f;
            const float resistancePenalty = -0.6f;
            const string sentence =
                "+1% to Poison Damage Multiplier per point of Poison Resistance above the cap. 60% less Poison Resistance.";

            UseEnglishCatalog();

            var skill = new Battle.Source.PassiveSkills.ViciousBitePassiveSkill(perOvercap, resistancePenalty);

            Assert.AreEqual(sentence, WithoutMarkup(skill.Description));
        }

        /// <summary>The keystone wave of the pass, by the ids its factories answer to.</summary>
        private static readonly string[] s_keystoneWave =
        [
            "Passive_Skill_Iron_Will",
            "Passive_Skill_Wind_Of_Freedom",
            Battle.Source.PassiveSkills.AgnosticPassiveSkill.PassiveId,
            Battle.Source.PassiveSkills.StoicismPassiveSkill.PassiveId,
            Battle.Source.PassiveSkills.ViciousBitePassiveSkill.PassiveId,
            Battle.Source.PassiveSkills.GiftOfNaturePassiveSkill.PassiveId,
            Battle.Source.PassiveSkills.StrengthOfSpiritPassiveSkill.PassiveId,
            Battle.Source.PassiveSkills.ElementalFuryPassiveSkill.PassiveId,
            Battle.Source.PassiveSkills.TrinityPassiveSkill.PassiveId,
        ];

        /// <summary>A share the eye reads as a share: 0.5 renders as "50%" under <c>{X:%}</c> and as "0.5"
        /// plain, so neither shape of placeholder can pass by rendering an empty string.</summary>
        private static RecordProperties KeystoneFields(string id, Core.Battle.Skills.PassiveSkillCatalog catalog)
        {
            var values = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (string field in catalog.RequiredFields(id)) values[field] = 0.5f;

            return new RecordProperties(id, values);
        }

        private static Core.Battle.Skills.PassiveSkillCatalog ShippedPassiveCatalog()
        {
            string path = Path.Combine(LastBreathTest.SharedData.Catalog(DataCatalog.PassiveSkills), "PassiveCatalog.json");
            Assert.IsTrue(File.Exists(path), $"the passive catalog ships at {path}");

            return Core.Battle.Skills.PassiveSkillCatalog.Read(File.ReadAllText(path));
        }

        /// <summary>A rendered description without its BBCode tint — the sentence a player reads.</summary>
        private static string WithoutMarkup(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text, @"\[/?[^\[\]]*\]", string.Empty);

        /// <summary>The shipped wording, pinned to the static facade every card reads through.</summary>
        private static void UseEnglishCatalog() => UseCatalog("en.po");

        private static void UseCatalog(string poFileName)
        {
            var catalog = new FakeLocalizationProvider();
            foreach ((string key, string wording) in ReadEntries(poFileName)) catalog.Strings[key] = wording;

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

        /// <summary>
        /// The amulet's damage-over-time line as a player reads it — shipped pool, shipped units, shipped
        /// wording, both as the pool's spread and as one rolled line. The catalog stores the bonus as a
        /// fraction, so a parameter left out of the unit table reaches the card as "+0.1", a number nobody
        /// can act on and one that looks like a rounding bug rather than a missing entry.
        /// </summary>
        [TestMethod]
        public void TheAmuletsDamageOverTimeLineReadsAsAPercentInTheShippedData()
        {
            const string pool = "Amulet";
            const string wording = "All Damage over Time Multiplier";

            var formats = new ParameterFormatProvider();
            new GameDataService(new FileSystemDataSource(LastBreathTest.SharedData.Root()), [formats]).LoadAll();

            var catalog = new FakeLocalizationProvider();
            foreach ((string key, string text) in ReadEntries("en.po")) catalog.Strings[key] = text;
            var formatter = new ModifierFormatter(catalog, formats);

            ParameterDescriptor entry = ShippedPool(pool)
                .OfType<ParameterDescriptor>()
                .Single(descriptor => descriptor.Parameter == EntityParameter.AllDoTDamageMultiplier);

            Assert.AreEqual($"+5–15% to {wording}", formatter.FormatDescriptor(entry),
                "the pool's spread does not reach the player as a percentage");

            var sink = new CollectingSink();
            new ModifierMaterializer(new DefaultRandomNumberGenerator(Seed)).Materialize(entry, sink, pool);
            string line = formatter.Format(sink.Entities.Single());

            StringAssert.EndsWith(line, $"% to {wording}", $"a rolled line of the amulet reads: {line}");
        }

        /// <summary>
        /// The flat twins name their parameter with "to" in English, percent and plain number alike:
        /// nothing else stands between the number and the name, so "+15% Critical Chance" runs together
        /// into one noun phrase. The worded twins already carry a joining word and must not gain a second
        /// one. A flat penalty keeps its minus and the same wording — "-25 to X" is the sentence read
        /// backwards, not a different one.
        /// </summary>
        [TestMethod]
        public void TheFlatLineNamesItsParameterWithToInTheShippedCatalog()
        {
            var formats = new ParameterFormatProvider();
            new GameDataService(new FileSystemDataSource(LastBreathTest.SharedData.Root()), [formats]).LoadAll();

            var catalog = new FakeLocalizationProvider();
            foreach ((string key, string wording) in ReadEntries("en.po")) catalog.Strings[key] = wording;
            var formatter = new ModifierFormatter(catalog, formats);

            Assert.AreEqual("+15% to Critical Chance",
                formatter.Format(new Modifier(ModifierValueType.Flat, EntityParameter.CriticalChance, 0.15f)),
                "a percent flat line still runs its number into the parameter name");
            Assert.AreEqual("+25 to Strength",
                formatter.Format(new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 25f)),
                "a plain flat line still runs its number into the parameter name");
            Assert.AreEqual("+40–60 to Strength",
                formatter.FormatRanged(new Modifier(ModifierValueType.Flat, EntityParameter.Strength, 50f), 0.8f, 1.2f),
                "a flat spread words itself differently from the single value it rolls into");
            Assert.AreEqual("-25 to Strength",
                formatter.Format(new Modifier(ModifierValueType.Flat, EntityParameter.Strength, -25f)),
                "a flat penalty drops the wording its bonus twin uses");
            Assert.AreEqual("+10% increased Physical Damage",
                formatter.Format(new Modifier(ModifierValueType.Increase, EntityParameter.PhysicalDamage, 0.1f)),
                "a worded twin picked up a joining word it already had");
        }

        /// <summary>The "to" is English wording, not line structure: Russian words its own templates and
        /// this pass left them alone.</summary>
        [TestMethod]
        public void TheRussianModifierTemplatesAreUntouchedByTheEnglishWording()
        {
            Dictionary<string, string> ru = ReadEntries("ru.po");

            foreach (string key in new[] { "Modifier_Flat", "Modifier_Flat_Range", "Modifier_Flat_PerParameter" })
            {
                Assert.IsTrue(ru.ContainsKey(key), $"ru.po lost '{key}' — the line would print its raw key");
                StringAssert.DoesNotMatch(ru[key], new System.Text.RegularExpressions.Regex(@"\bto\b"),
                    $"ru.po '{key}' carries the English joining word: {ru[key]}");
            }
        }

        /// <summary>One shipped pool through the real parser — the entries the game actually rolls.</summary>
        private static IReadOnlyList<IModifierDescriptor> ShippedPool(string poolId)
        {
            var parser = new DataParser(new ItemGameDataFactory());
            foreach (string file in Directory.EnumerateFiles(
                LastBreathTest.SharedData.Catalog(DataCatalog.ModifierPools), "*.json", SearchOption.AllDirectories))
            {
                if (parser.ParseEquipItemModifierPools(File.ReadAllText(file)).TryGetValue(poolId, out var pool)) return pool;
            }

            Assert.Fail($"the shipped pools carry no '{poolId}'");
            return [];
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
            // The trade window titles itself by the trader's id, so an unworded shop shows the raw key
            ("Traders", CatalogIds("Traders", "traders"), false),
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
