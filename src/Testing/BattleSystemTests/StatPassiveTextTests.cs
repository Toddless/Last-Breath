namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Battle.Source.PassiveSkills;
    using Core.Battle;
    using Core.Battle.Skills;
    using Core.Enums;
    using Core.Localization;
    using Core.PassiveTree;
    using Core.PassiveTree.View;

    /// <summary>
    /// What a node that hands over a PASSIVE says, and what the passive itself says — one reading or two.
    /// A keystone made of stats is a record of fields: nobody writes rule text for it, so the numbers have
    /// to word themselves, through the very templates an ordinary node line goes through. A keystone
    /// written as a class has rule text, and the node reads it out instead of guessing.
    /// <para>The trap this file exists to catch is drift: a popup that parsed the field grammar a second
    /// time, or a card that spelled the same numbers differently from the node the player clicked.</para>
    /// </summary>
    [TestClass]
    public class StatPassiveTextTests
    {
        private const string StatId = "Passive_Skill_Stats_Unlimited_Power";
        private const string ClawsId = "Passive_Skill_Poisoned_Claws";

        /// <summary>The two shapes of the grammar: a line measured per unit of a carrier, and a flat one.</summary>
        private const string PerStrength = "PhysicalDamage:Increase:Strength";

        private const string ArmorLine = "Armor:Flat";

        /// <summary>The third shape: a line counted in a step of its carrier rather than per unit of it.</summary>
        private const string PerHundredEvade = "CriticalDamage:Increase:Evade:100";

        /// <summary>The author meant PhysicalDamage. The grant refuses the whole passive over it; the popup
        /// must not go down with it.</summary>
        private const string Typo = "PhysicalDamge:Increase";

        private const string ScaledSentence = "+1% increased Physical Damage per Strength";

        /// <summary>The step stands where the carrier's name alone used to, and nowhere else in the
        /// sentence — the wording is the shipped template with a longer {per}.</summary>
        private const string SteppedSentence = "+2% increased Critical Damage per 100 Evade";

        /// <summary>The flat twin names its parameter with "to" — without it the number and the name
        /// collide into one noun phrase.</summary>
        private const string FlatSentence = "+20 to Armor";

        /// <summary>
        /// A stat node prints its record through the game's own modifier wording, both shapes of it, in the
        /// order the record wrote them. This is the whole point of phase 2.5: before it the popup on such a
        /// node was empty, because the reading only knew about modifier lines.
        /// </summary>
        [TestMethod]
        public void AStatNodePrintsEveryFieldOfItsRecordInTheShippedWording()
        {
            PassiveNode node = StatNode();

            string[] lines = Lines(node);

            CollectionAssert.AreEqual(new[] { ScaledSentence, FlatSentence }, lines,
                $"the stat node reads: {string.Join(" | ", lines)}");
        }

        /// <summary>
        /// A line counted in a step of its carrier says so: the count stands in front of the carrier's name,
        /// inside the very "per {per}" tail the wording already had, so no template was written twice.
        /// </summary>
        [TestMethod]
        public void ASteppedLineNamesTheCountOfItsCarrier()
        {
            var node = new PassiveNode { Id = "keystone_stats", Kind = PassiveNodeKind.Keystone, PassiveId = StatId };
            node.Properties[PerHundredEvade] = 0.02f;

            string[] lines = Lines(node);

            CollectionAssert.AreEqual(new[] { SteppedSentence }, lines, $"the stepped node reads: {string.Join(" | ", lines)}");
        }

        /// <summary>The regression a step must not cause: a line that names none reads exactly as it always
        /// did, down to the word.</summary>
        [TestMethod]
        public void ALineWithNoStepReadsAsItAlwaysDid()
        {
            var node = new PassiveNode { Id = "keystone_stats", Kind = PassiveNodeKind.Keystone, PassiveId = StatId };
            node.Properties[PerStrength] = 0.01f;

            CollectionAssert.AreEqual(new[] { ScaledSentence }, Lines(node));
        }

        /// <summary>
        /// The popup and the grant read ONE grammar. A second parser written for the UI would drift the
        /// first time a shape was added to either side, and the node would promise a line the character
        /// never gets — or hide one he does.
        /// </summary>
        [TestMethod]
        public void ThePopupAndTheGrantReadTheSameFields()
        {
            PassiveNode node = StatNode();

            var granted = StatPassiveSkill.Create(StatId, new RecordProperties(StatId, node.Properties));
            List<StatPassiveLine> printed = StatPassiveGrammar.ReadWhole(node.Properties);

            CollectionAssert.AreEqual(granted.Lines.ToList(), printed,
                "the wheel and the grant no longer read a record the same way");
        }

        /// <summary>
        /// A field the grammar cannot read costs the GRANT the whole passive, so it costs the popup the
        /// whole card. The two readable fields beside it are NOT printed: a player shown two of three lines
        /// would buy the node for them and be handed none of the three, and the refusal that explains it
        /// goes to a log he is not reading.
        /// </summary>
        [TestMethod]
        public void AFieldTheGrammarCannotReadCostsThePopupTheWholeCard()
        {
            PassiveNode node = StatNode();
            node.Properties[Typo] = 0.5f;

            string[] lines = Lines(node);

            Assert.AreEqual(0, lines.Length,
                $"the popup promised lines the grant is about to refuse: {string.Join(" | ", lines)}");
            Assert.IsNull(new PassiveSkillProvider().CreateSkill(StatId, new RecordProperties(StatId, node.Properties)),
                "the grant handed out a passive missing the line the author wrote");
        }

        /// <summary>A record whose every field is a typo has nothing to print, and still prints it without
        /// throwing — the popup is left with its title and the node's class.</summary>
        [TestMethod]
        public void ARecordNobodyCanReadPrintsNothingRatherThanThrowing()
        {
            var node = new PassiveNode { Id = "keystone_typo", PassiveId = StatId };
            node.Properties[Typo] = 0.5f;

            Assert.AreEqual(0, Lines(node).Length);
        }

        /// <summary>A passive written as a class carries hand-written rule text, and the node reads that
        /// out: its numbers live inside the class, and a node guessing at them would print a second,
        /// unrelated set.</summary>
        [TestMethod]
        public void ANamedPassiveNodeReadsOutTheCatalogsDescription()
        {
            var node = new PassiveNode { Id = "keystone_claws", PassiveId = ClawsId };
            node.Properties["percentFromDamage"] = 0.75f;

            FakeLocalizationProvider catalog = Catalog();
            string[] lines = Lines(node, catalog);

            Assert.AreEqual(1, lines.Length, $"the named passive reads: {string.Join(" | ", lines)}");
            Assert.AreEqual(catalog.Strings[ClawsId + LocalizationService.DescriptionSuffix], lines[0]);
            Assert.AreEqual(catalog.Strings[ClawsId], PassiveNodeLines.TitleOf(node, catalog),
                "an untitled passive node is headed by something other than the passive it hands over");
        }

        /// <summary>
        /// The card of the passive and the node that hands it over say the same thing, word for word. A
        /// stat passive has no <c>_Description</c> anybody wrote — an author names a fresh one without
        /// writing code — so the card is built from the lines, through the same formatter the wheel uses.
        /// </summary>
        [TestMethod]
        public void TheCardOfAStatPassiveSaysWhatItsNodeSaid()
        {
            PassiveNode node = StatNode();
            FakeLocalizationProvider catalog = Catalog();
            var formatter = new ModifierFormatter(catalog, new ParameterFormatProvider());

            Localization.Override(new LocalizationService(catalog, formatter, new ContextModifierFormatter(catalog),
                [new StatPassiveLineTextFormatter(formatter)]));

            var skill = StatPassiveSkill.Create(StatId, new RecordProperties(StatId, node.Properties));
            string fromTheNode = string.Join(
                StatPassiveLineText.LineSeparator,
                PassiveNodeLines.Of(node, formatter, null, catalog, TextFormat.Rich).Select(line => line.Text));

            Assert.AreEqual(fromTheNode, skill.Description,
                "the passive's card and the node it came from word the same numbers differently");
            StringAssert.Contains(skill.Description, "1%", "the card lost the number the record was tuned with");
            StringAssert.Contains(skill.Description, "20", "the card lost the number the record was tuned with");
        }

        /// <summary>
        /// An untitled node naming BOTH a passive and an ability is headed by the passive. Nothing in the
        /// data forbids such a node — the channels that collide are a passive and modifier LINES — and the
        /// passive is what the node hands over, while the ability id on it is the ring's address.
        /// </summary>
        [TestMethod]
        public void AnUntitledNodeNamingBothIsHeadedByThePassiveItHandsOver()
        {
            var node = new PassiveNode { Id = "keystone_both", PassiveId = ClawsId, AbilityId = "Ability_Dex" };

            FakeLocalizationProvider catalog = Catalog();

            Assert.AreEqual(catalog.Strings[ClawsId], PassiveNodeLines.TitleOf(node, catalog),
                "the ability the node points at took the headline from the passive it grants");
        }

        /// <summary>
        /// The one line of the container that carries all of this to the player, and the one thing no walk
        /// above can prove: with the formatter unregistered, <see cref="ILocalizationService.Format"/>
        /// finds nobody who answers for a stat line and returns an empty string — the card of every stat
        /// passive goes blank, no build fails and nothing throws.
        /// <para>Read off the shared root rather than out of a live composition on purpose: the container
        /// is a first-wins singleton this suite already bootstraps for its own canons, and initializing it
        /// here would decide the whole run's composition. Same road
        /// <see cref="AugmentCatalogOutsideBattleTests"/> takes for the same kind of silent line.</para>
        /// </summary>
        [TestMethod]
        public void TheSharedRootRegistersTheFormatterThatWordsAStatLine()
        {
            string root = Path.Combine(SrcRoot, "Core", "Services", "GameServiceProvider.cs");
            Assert.IsTrue(File.Exists(root), $"the shared composition root is not where it lived: {root}");

            string registration =
                $"AddSingleton<{nameof(ITextFormatter)}, {nameof(StatPassiveLineTextFormatter)}>()";

            StringAssert.Contains(File.ReadAllText(root), registration,
                $"the shared root no longer registers '{registration}', so every stat passive's card is empty");
        }

        /// <summary>The regression the passive branch must not cause: a node that speaks in LINES speaks in
        /// them exactly as before, gate and all.</summary>
        [TestMethod]
        public void AnOrdinaryNodeStillSpeaksInItsLines()
        {
            var node = new PassiveNode { Id = "small_str_1" };
            node.Modifiers.Add(new ModifierLine
            {
                Parameter = EntityParameter.Armor, ValueType = ModifierValueType.Flat, Value = 20f
            });

            string[] lines = Lines(node);

            CollectionAssert.AreEqual(new[] { FlatSentence }, lines, $"the line node reads: {string.Join(" | ", lines)}");
        }

        /// <summary>The source tree above the test's own output, for the walk that reads a registration
        /// nothing else can prove.</summary>
        private static string SrcRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                    directory = directory.Parent;

                Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
                return directory.FullName;
            }
        }

        /// <summary>The node phase 2.5 is about: a keystone whose whole content is two fields.</summary>
        private static PassiveNode StatNode()
        {
            var node = new PassiveNode { Id = "keystone_stats", Kind = PassiveNodeKind.Keystone, PassiveId = StatId };
            node.Properties[PerStrength] = 0.01f;
            node.Properties[ArmorLine] = 20f;
            return node;
        }

        private static string[] Lines(PassiveNode node, FakeLocalizationProvider? catalog = null)
        {
            FakeLocalizationProvider strings = catalog ?? Catalog();
            return
            [
                .. PassiveNodeLines.Of(node, new ModifierFormatter(strings, new ParameterFormatProvider()), null, strings)
                    .Select(line => line.Text)
            ];
        }

        /// <summary>The shipped wording, so the sentences pinned above are the ones the player sees rather
        /// than a fixture agreeing with itself.</summary>
        private static FakeLocalizationProvider Catalog()
        {
            var catalog = new FakeLocalizationProvider();
            string[] lines = File.ReadAllLines(Path.Combine(SharedData.Root(), "Localization", "en.po"));

            for (int line = 0; line + 1 < lines.Length; line++)
            {
                if (!Quoted(lines[line], "msgid ", out string id) || id.Length == 0) continue;
                if (Quoted(lines[line + 1], "msgstr ", out string text)) catalog.Strings[id] = text;
            }

            Assert.IsTrue(catalog.Strings.Count > 0, "en.po was not read at all, so these sentences prove nothing");
            return catalog;
        }

        private static bool Quoted(string line, string prefix, out string value)
        {
            value = string.Empty;
            if (!line.StartsWith(prefix + '"', StringComparison.Ordinal) || !line.EndsWith('"')) return false;

            value = line[(prefix.Length + 1)..^1];
            return true;
        }
    }
}
