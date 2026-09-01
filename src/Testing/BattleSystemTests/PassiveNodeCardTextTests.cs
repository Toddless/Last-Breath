namespace LastBreathTest.BattleSystemTests
{
    using System.Text.RegularExpressions;
    using Battle.Source;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Data.GameData;
    using Core.Localization;
    using Core.PassiveTree;
    using Core.PassiveTree.View;

    /// <summary>
    /// What a node handing over a NAMED passive says, and who says it. The passive's rule text is a
    /// template — "{PercentFromDamage:%} ... for {PoisonDuration|turn|turns}" — and phase 2.5 left the
    /// wheel reading that key out raw, so a keystone advertised itself in its own placeholders. It says
    /// its numbers now because the popup builds the very passive the click would hand over, from the pair
    /// the grant is built from, and shows that passive's card.
    /// <para>The authoring tool cannot reach the passive classes at all, so it fills the same template from
    /// the same rules (<see cref="PassiveDisplayValues"/>). That the two roads end at one sentence is the
    /// last walk here, taken over every named passive the catalog ships.</para>
    /// </summary>
    [TestClass]
    public class PassiveNodeCardTextTests
    {
        /// <summary>The keystone of the report: "Poisonous Bite", tuned on its node to 0.75 and 3.</summary>
        private const string PoisonousBite = "keystone_7";

        private const string ClawsId = "Passive_Skill_Poisoned_Claws";

        /// <summary>A shipped node's numbers as the player reads them — the two the report says were
        /// missing.</summary>
        private const string ShippedShare = "75%";

        private const string ShippedDuration = "3 turns";

        /// <summary>Distinct per field, so a rule pointing at the wrong one cannot pass by coincidence, and
        /// whole, because half of these fields reach a factory as counts: a fraction would separate the two
        /// readings over how a turn count is rounded rather than over the mapping this walk is about.</summary>
        private static readonly float[] s_sentinels = [3f, 5f, 7f, 11f];

        /// <summary>
        /// Pin 1. The shipped tree, the shipped wording and the real registry: the popup of the keystone
        /// prints the numbers ITS node carries and keeps no placeholder of the template it was written as.
        /// </summary>
        [TestMethod]
        public void TheKeystonesPopupPrintsTheNumbersItsOwnNodeIsTunedWith()
        {
            FakeLocalizationProvider catalog = UseEnglish();
            PassiveNode node = ShippedNode(PoisonousBite);

            Assert.AreEqual(ClawsId, node.PassiveId, $"'{PoisonousBite}' no longer hands over the passive this pins");

            string line = WithoutMarkup(Popup(node, catalog));

            StringAssert.Contains(line, ShippedShare, $"the popup lost the node's share: {line}");
            StringAssert.Contains(line, ShippedDuration, $"the popup lost the node's duration: {line}");
            Assert.IsFalse(line.Contains('{'), $"the popup still prints the template at the player: {line}");
        }

        /// <summary>
        /// Pin 2. A second node of the same passive, tuned differently, reads with ITS numbers — the popup
        /// is built from the node and never from whatever the catalog or a class was written with.
        /// </summary>
        [TestMethod]
        public void ASecondNodeOfOnePassivePrintsItsOwnNumbersAndNotTheFirstOnes()
        {
            FakeLocalizationProvider catalog = UseEnglish();

            var node = new PassiveNode { Id = "keystone_other_bite", PassiveId = ClawsId };
            node.Properties["percentFromDamage"] = 0.4f;
            node.Properties["duration"] = 5f;

            string line = WithoutMarkup(Popup(node, catalog));
            string shipped = WithoutMarkup(Popup(ShippedNode(PoisonousBite), catalog));

            StringAssert.Contains(line, "40%", $"the second node borrowed somebody else's share: {line}");
            StringAssert.Contains(line, "5 turns", $"the second node borrowed somebody else's duration: {line}");
            Assert.AreNotEqual(shipped, line, "two differently tuned nodes of one passive read the same");
            Assert.IsFalse(line.Contains('{'), $"the popup still prints the template at the player: {line}");
        }

        /// <summary>
        /// Every passive-bearing node the tree ships, read the way the wheel reads it. A keystone of the
        /// wave whose text names a value nobody hands it would print that token, and this is where a fresh
        /// one is caught — including the two the wave added last (a price stated as a magnitude, and a
        /// keystone carrying no fields at all).
        /// </summary>
        [TestMethod]
        public void NoPassiveBearingNodeOfTheShippedTreeKeepsAPlaceholder()
        {
            FakeLocalizationProvider catalog = UseEnglish();
            PassiveTreeDocument tree = ShippedTree();
            var complaints = new List<string>();

            foreach (PassiveNode node in tree.Nodes)
            {
                if (node.PassiveId is not { Length: > 0 }) continue;

                string line = string.Join(" | ", PopupLines(node, catalog));
                if (line.Contains('{')) complaints.Add($"{node.Id} ({node.PassiveId}): {line}");
            }

            Assert.AreEqual(0, complaints.Count, string.Join("\n", complaints));
        }

        /// <summary>
        /// Pin 4. The authoring tool's tooltip and the game's popup are one sentence: the tool renders the
        /// same template from the same rules and only leaves the decoration off — a keyword arrives as the
        /// bare word it names, because a tool has no tooltip window to open on a click.
        /// </summary>
        [TestMethod]
        public void TheToolsTooltipReadsTheCardWithoutItsDecoration()
        {
            FakeLocalizationProvider catalog = UseEnglish();
            PassiveNode node = ShippedNode(PoisonousBite);

            string fromTheTool = PassiveDisplayValues.Describe(node.PassiveId!, node.Properties, catalog);

            Assert.AreEqual(WithoutMarkup(Popup(node, catalog)), fromTheTool,
                "the tool and the wheel word one keystone two ways");
            StringAssert.Contains(fromTheTool, ShippedShare, $"the tool lost the node's share: {fromTheTool}");
            StringAssert.Contains(fromTheTool, ShippedDuration, $"the tool lost the node's duration: {fromTheTool}");
            StringAssert.Contains(fromTheTool, catalog.Translate("Poison"),
                $"the tool dropped the keyword instead of naming it: {fromTheTool}");
            Assert.IsFalse(fromTheTool.Contains('{'), $"the tool still prints the template: {fromTheTool}");
        }

        /// <summary>
        /// The walk that keeps the tool honest for every passive and not just for the one in the report:
        /// each named id the catalog ships, tuned with a distinct number per field, worded once by the
        /// PASSIVE the registry builds and once by the RULES the tool renders through. A name the tool
        /// spells differently, a field it reads off the wrong key, a derived figure whose sign it turns the
        /// wrong way — all of them are one sentence apart here.
        /// </summary>
        [TestMethod]
        public void EveryNamedPassiveWordsOneSentenceForTheToolAndForTheGame()
        {
            FakeLocalizationProvider catalog = UseEnglish();
            PassiveSkillCatalog shipped = ShippedPassiveCatalog();
            var registry = new PassiveSkillProvider();
            var complaints = new List<string>();

            foreach (string id in shipped.Ids)
            {
                Dictionary<string, float> values = Sentinels(shipped.RequiredFields(id));

                ISkill? skill = registry.CreateSkill(id, new RecordProperties(id, values));
                Assert.IsNotNull(skill, $"the registry no longer builds '{id}' from the fields the catalog declares");

                string fromTheGame = skill.Describe(TextFormat.Plain);
                string fromTheTool = PassiveDisplayValues.Describe(id, values, catalog);

                if (fromTheGame != fromTheTool)
                    complaints.Add($"'{id}': the game says \"{fromTheGame}\", the tool says \"{fromTheTool}\"");
            }

            Assert.IsTrue(shipped.Ids.Count > 0, "the shipped passive catalog was not read at all");
            Assert.AreEqual(0, complaints.Count, string.Join("\n", complaints));
        }

        /// <summary>The word a record writes a PRICE under. The convention is the data's, not this file's:
        /// a penalty is stored as the negative share its modifier is built from.</summary>
        private const string PriceField = "Penalty";

        /// <summary>
        /// The sign of a price, walked over the WHOLE table rather than pinned passive by passive. A rule
        /// restating a penalty under a second name — the "cut" a card states — is owed the MAGNITUDE,
        /// because the sentence around it already says "less"; one that stopped turning the share around
        /// would print "-45% less" — the cost written twice, pointing both ways — and would read perfectly
        /// well to every other walk in this file, since the game fills its card from this same table.
        /// <para>What is asked of a row is decided by the FIELD it reads and by the name differing from
        /// that field's own — never by the turning-around flag itself, which a mistake would have removed
        /// along with the row. A keystone added later with a price of its own is covered the moment its
        /// rule is written, without a line here naming it.</para>
        /// </summary>
        [TestMethod]
        public void EveryRestatedPriceOfTheTableReachesTheReaderAsAMagnitude()
        {
            const float storedShare = -0.6f;
            var complaints = new List<string>();
            var walked = 0;

            foreach ((string passiveId, string name, string field, bool _) in PassiveDisplayValues.Rules())
            {
                // The price under its own name is the share as stored; only a SECOND name for it restates
                // the cost, and that is the one a sentence saying "less" reads the magnitude of.
                if (!field.EndsWith(PriceField, StringComparison.Ordinal) || name == Pascal(field)) continue;

                walked++;
                var record = new Dictionary<string, float>(StringComparer.Ordinal) { [field] = storedShare };
                object? shown = PassiveDisplayValues.Of(passiveId, record).GetValueOrDefault(name);

                if (shown is not float magnitude || magnitude <= 0f)
                    complaints.Add($"'{passiveId}'.{name} reaches the card as '{shown}' where '{field}' stores {storedShare}");
            }

            Assert.IsTrue(walked > 0, "the table restates no price at all, so this walk proves nothing");
            Assert.AreEqual(0, complaints.Count, string.Join("\n", complaints));
        }

        /// <summary>A record field under the name a template would ask for it by, when it asks for the
        /// field itself rather than for something read off it.</summary>
        private static string Pascal(string field) => char.ToUpperInvariant(field[0]) + field[1..];

        /// <summary>
        /// The registry is there and still hands nothing back: an id nobody builds, and an id whose factory
        /// is short a field the record never carried. Both are refused loudly by the provider and neither
        /// may take the popup down or blank it — the node is left with the wording as it stands, which is
        /// the reading this branch had before it could build at all.
        /// </summary>
        [TestMethod]
        public void ANodeTheRegistryCannotBuildFallsBackToTheWordingRatherThanToNothing()
        {
            FakeLocalizationProvider catalog = UseEnglish();

            var unknown = new PassiveNode { Id = "keystone_unknown", PassiveId = "Passive_Skill_Nobody_Wrote" };
            Assert.AreEqual("Passive_Skill_Nobody_Wrote" + LocalizationService.DescriptionSuffix, Popup(unknown, catalog),
                "an id no factory answers for took the popup somewhere other than the untouched key");

            // The claws need a share and a duration; a node carrying neither refuses the grant entire.
            var starved = new PassiveNode { Id = "keystone_starved", PassiveId = ClawsId };
            string line = Popup(starved, catalog);

            Assert.IsNull(new PassiveSkillProvider().CreateSkill(ClawsId, new RecordProperties(ClawsId, starved.Properties)),
                "the grant now builds a passive out of fields the node never wrote, so this case proves nothing");
            Assert.AreEqual(catalog.Strings[ClawsId + LocalizationService.DescriptionSuffix], line,
                "a node the registry refused printed something other than the wording as it stands");
        }

        /// <summary>The reading left where no registry can be asked — a document opened by something that
        /// composes no battle. The wording is read out untouched rather than left blank.</summary>
        [TestMethod]
        public void APopupWithNoRegistryBehindItStillReadsTheWordingOut()
        {
            FakeLocalizationProvider catalog = UseEnglish();
            PassiveNode node = ShippedNode(PoisonousBite);

            string[] lines = [.. PassiveNodeLines.Of(node, null, null, catalog).Select(line => line.Text)];

            Assert.AreEqual(1, lines.Length);
            Assert.AreEqual(catalog.Strings[ClawsId + LocalizationService.DescriptionSuffix], lines[0]);
        }

        /// <summary>The popup's own line for a node handing over a named passive, built the way the wheel
        /// builds it: the real registry, the shipped wording, the node's own properties.</summary>
        private static string Popup(PassiveNode node, FakeLocalizationProvider catalog)
        {
            List<string> lines = PopupLines(node, catalog);

            Assert.AreEqual(1, lines.Count, $"'{node.Id}' speaks in {lines.Count} lines, not one");
            return lines[0];
        }

        /// <summary>Everything the popup says about a node — a named passive answers in one line, a passive
        /// made of stat fields in one per field.</summary>
        private static List<string> PopupLines(PassiveNode node, FakeLocalizationProvider catalog) =>
        [
            .. PassiveNodeLines.Of(
                    node,
                    new ModifierFormatter(catalog, new ParameterFormatProvider()),
                    null,
                    catalog,
                    TextFormat.Rich,
                    new PassiveSkillProvider())
                .Select(line => line.Text)
        ];

        /// <summary>One distinct number per field. Refuses rather than wrapping: a passive with a fifth
        /// field would silently be handed the first number twice, and two fields sharing a value is exactly
        /// the coincidence these sentinels exist to rule out.</summary>
        private static Dictionary<string, float> Sentinels(IReadOnlyList<string> fields)
        {
            Assert.IsTrue(fields.Count <= s_sentinels.Length,
                $"a passive now declares {fields.Count} fields and there are only {s_sentinels.Length} distinct " +
                "sentinels to tell them apart — add one rather than letting the values repeat");

            var values = new Dictionary<string, float>(StringComparer.Ordinal);
            for (int index = 0; index < fields.Count; index++) values[fields[index]] = s_sentinels[index];

            return values;
        }

        private static PassiveNode ShippedNode(string id)
        {
            PassiveNode? node = ShippedTree().Find(id);
            Assert.IsNotNull(node, $"the shipped tree carries no '{id}' to read");

            return node!;
        }

        private static PassiveTreeDocument ShippedTree()
        {
            var tree = new PassiveTreeProvider();
            new GameDataService(new FileSystemDataSource(LastBreathTest.SharedData.Root()), [tree]).LoadAll();

            return tree.Tree;
        }

        private static PassiveSkillCatalog ShippedPassiveCatalog()
        {
            string path = Path.Combine(LastBreathTest.SharedData.Catalog(DataCatalog.PassiveSkills), "PassiveCatalog.json");
            Assert.IsTrue(File.Exists(path), $"the passive catalog ships at {path}");

            return PassiveSkillCatalog.Read(File.ReadAllText(path));
        }

        /// <summary>The shipped English wording, pinned both to the provider these walks hand around and to
        /// the static facade a passive's own card reads through — one catalog, so the two roads compared
        /// here cannot differ by reading different files.</summary>
        private static FakeLocalizationProvider UseEnglish()
        {
            var catalog = new FakeLocalizationProvider();
            string[] lines = File.ReadAllLines(Path.Combine(LastBreathTest.SharedData.Root(), "Localization", "en.po"));

            for (int line = 0; line + 1 < lines.Length; line++)
            {
                if (!Quoted(lines[line], "msgid ", out string id) || id.Length == 0) continue;
                if (Quoted(lines[line + 1], "msgstr ", out string text)) catalog.Strings[id] = text;
            }

            Assert.IsTrue(catalog.Strings.Count > 0, "en.po was not read at all, so these sentences prove nothing");

            var formatter = new ModifierFormatter(catalog, new ParameterFormatProvider());
            Localization.Override(new LocalizationService(catalog, formatter, new ContextModifierFormatter(catalog),
                [new StatPassiveLineTextFormatter(formatter)]));

            return catalog;
        }

        private static bool Quoted(string line, string prefix, out string value)
        {
            value = string.Empty;
            string trimmed = line.Trim();
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal)) return false;

            string rest = trimmed[prefix.Length..].Trim();
            if (rest.Length < 2 || rest[0] != '"' || rest[^1] != '"') return false;

            value = rest[1..^1];
            return true;
        }

        /// <summary>A rendered sentence without its BBCode tint — the words a player reads.</summary>
        private static string WithoutMarkup(string text) => Regex.Replace(text, @"\[/?[^\[\]]*\]", string.Empty);
    }
}
