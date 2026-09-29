namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.Schema;
    using Core.Narrative.Facts;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Schema.Model;

    /// <summary>
    /// What an author is offered under a field holding a fact key, read over the shipped documents: the
    /// families the game's own code keeps, written with their parameters named, and every word the
    /// dialogues and the quests already write or ask about.
    /// <para>An OPEN list, which is the whole of what these hold: a word nobody has met is refused by
    /// nothing here, and the only thing the tool says about one is that it has not met it.</para>
    /// </summary>
    [TestClass]
    public class FactKeySuggestionsTests
    {
        /// <summary>The counter the four trials are written around: one family the code keeps, and one key
        /// per beast the quests count. Both have to be offered — the template is how a key of the family is
        /// spelled, and the written ones are the beasts an author is choosing between.</summary>
        private const string KillQuery = "Kill";

        private const string KillFamily = "Kill_Count:<npcId>";

        private const string WolfKey = "Kill_Count:Npc_Wolf";

        private const string BearKey = "Kill_Count:Npc_Bear";

        /// <summary>A word spelled as nobody spells it: the same keys have to come back, because an author
        /// hunting for a key remembers the word and not its capitals.</summary>
        private const string ShoutedQuery = "kILL_cOUNT:npc_wOLF";

        private const string OtherSource = "somethingElse";

        private static FactKeySuggestions s_facts = null!;

        [ClassInitialize]
        public static void Read(TestContext context)
        {
            string root = SharedData.Root();
            var workspace = CatalogWorkspace.Load(root, CatalogDescriptors.All);

            s_facts = new FactKeySuggestions(workspace, new ReferenceIndex(workspace), texts: null);
        }

        /// <summary>The one thing the whole list exists for: an author writing a counter is offered both how
        /// the family is spelled and the beasts somebody has already counted.</summary>
        [TestMethod]
        public void AQueryNamingAFamily_OffersItsTemplateAndTheKeysTheDocumentsWrite()
        {
            string[] offered = Words(s_facts.Matching(KillQuery));

            CollectionAssert.Contains(offered, KillFamily, Said(offered));
            CollectionAssert.Contains(offered, WolfKey, Said(offered));
            CollectionAssert.Contains(offered, BearKey, Said(offered));

            Assert.IsTrue(
                offered.All(key => key.Contains(KillQuery, StringComparison.OrdinalIgnoreCase)),
                $"a key nothing about the query was offered: {Said(offered)}");
        }

        /// <summary>An empty query is the author not remembering the word, which is exactly why he opened
        /// the list: everything the run met is offered, families and written words alike.</summary>
        [TestMethod]
        public void AnEmptyQuery_OffersEverythingTheRunMet()
        {
            string[] everything = Words(s_facts.Matching(string.Empty));

            CollectionAssert.Contains(everything, KillFamily, Said(everything));
            CollectionAssert.Contains(everything, WolfKey, Said(everything));

            Assert.IsTrue(everything.Length > s_facts.Matching(KillQuery).Count,
                "an empty query offered no more than one naming a single family");

            foreach (FactKeyDeclaration declaration in FactKeyDeclarations.All)
                CollectionAssert.Contains(everything, declaration.Template,
                    $"a family the code keeps is offered nowhere: {Said(everything)}");
        }

        /// <summary>The capitals of a key are the author's memory of it and not the question he is asking.</summary>
        [TestMethod]
        public void AQuerySpelledInOtherCapitals_OffersTheSameKeys()
        {
            string[] offered = Words(s_facts.Matching(ShoutedQuery));

            CollectionAssert.Contains(offered, WolfKey, Said(offered));
        }

        /// <summary>A family is offered as the family it is. The template is not a key — the game raises the
        /// members of a family and never the spelling of one — so the row standing for one has to be read
        /// apart from the words beside it, or an author picks a template and writes a word nothing answers.</summary>
        [TestMethod]
        public void AFamilyTheCodeKeeps_IsOfferedAsAFamilyAndTheWordsOfTheDocumentsAreNot()
        {
            IReadOnlyList<SuggestedWord> offered = s_facts.Matching(KillQuery);

            Assert.IsTrue(offered.Single(word => word.Word == KillFamily).Family,
                "the template of a family is offered as a word like any other");

            Assert.IsFalse(offered.Single(word => word.Word == WolfKey).Family,
                "a key a document writes is offered as a family");
        }

        /// <summary>
        /// What counts as a template nobody filled in is written twice — once for the game, which refuses
        /// such a word entry into the registry, and once for the tool, which paints the box holding one and
        /// marks the row offering it. The two are held to one answer over every word this run can put to
        /// them: a rule read one way by the game and another by the tool shows the author a key painted as
        /// met that the world will never raise.
        /// </summary>
        [TestMethod]
        public void WhatCountsAsAWordStillWaitingForItsParameter_IsOneRuleForTheGameAndTheTool()
        {
            string[] words =
            [
                .. FactKeyDeclarations.All.Select(declaration => declaration.Template),
                .. Words(s_facts.Matching(string.Empty)),
                string.Empty, WolfKey, FactKeys.KillCountHead, FactKeys.ItemEquippedAnyHead,
                "<npcId>", "Kill_Count:<", "Kill_Count:>", "Kill_Count:<npcId", "Kill_Count:npcId>"
            ];

            foreach (string word in words)
                Assert.AreEqual(FactKeyDeclarations.Unfilled(word), SuggestedWords.Unfilled(word),
                    $"the game and the tool read “{word}” differently");
        }

        /// <summary>The list answers for its own name and for no other. A source answering about words it
        /// does not keep would offer the world's facts under a field that has nothing to do with them.</summary>
        [TestMethod]
        public void AFieldAnsweredFromSomethingElse_IsOfferedNothing()
        {
            Assert.AreEqual(SuggestionSources.FactKeys, FactKeySuggestions.Source,
                "the name a schema writes and the name this answers to are one word");

            Assert.AreNotEqual(0, s_facts.For(FactKeySuggestions.Source, KillQuery).Count);
            Assert.AreEqual(0, s_facts.For(OtherSource, KillQuery).Count);
        }

        /// <summary>A run over a root holding no narrative at all still offers the families the code keeps:
        /// the world writes them whether or not one conversation has been authored yet, and an author
        /// opening a fresh quest is exactly who needs to see how a counter is spelled.</summary>
        [TestMethod]
        public void ARunOverNoNarrativeAtAll_StillOffersTheFamiliesTheCodeKeeps()
        {
            string root = EmptyRoot();
            var workspace = CatalogWorkspace.Load(root, CatalogDescriptors.All);
            var facts = new FactKeySuggestions(workspace, new ReferenceIndex(workspace), texts: null);

            string[] offered = Words(facts.Matching(string.Empty));

            CollectionAssert.Contains(offered, KillFamily, Said(offered));
            Assert.AreEqual(FactKeyDeclarations.All.Count, offered.Length,
                $"a word no document writes was offered: {Said(offered)}");
        }

        /// <summary>A data root with nothing written in it: no dialogue, no quest, no key of anybody's own.</summary>
        private static string EmptyRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "LastBreath", nameof(FactKeySuggestionsTests), "Empty");

            Directory.CreateDirectory(root);

            return root;
        }

        private static string[] Words(IReadOnlyList<SuggestedWord> offered) => [.. offered.Select(word => word.Word)];

        private static string Said(string[] offered) =>
            $"{offered.Length} key(s) offered:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", offered)}";
    }
}
