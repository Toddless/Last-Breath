namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Narrative.Validation;
    using Tooling.Catalogs.Checks;

    /// <summary>
    /// The gates over the shipped npc catalog. Both are held on the rules the authoring tool reports with
    /// and nowhere else: a word outside the members its field offers, a name written twice and a record
    /// naming no stance are the catalog checks' answer, and a species written able to talk with nothing to
    /// say is the narrative's. What an author sees in a Checks panel and what fails here is one answer.
    /// </summary>
    /// <remarks>The talking species used to be Inconclusive — a dialogue that may simply not be written
    /// yet. It is a gate now: the claim is made in the npc catalog, so a species declaring it and no
    /// dialogue behind it is data that contradicts itself, and the player clicking such an npc opens
    /// nothing.</remarks>
    [TestClass]
    public class NpcDataAuditTests
    {
        /// <summary>Where the narrative addresses what an npc record claims about itself.</summary>
        private const string NpcPlace = DataCatalog.Npc + "/";

        /// <summary>A record naming no stance at all: the provider falls back to a default nobody chose,
        /// and the behaviour archetype follows the stance.</summary>
        private const string StancelessNpcJson =
            """
            { "npcs": [ { "id": "Npc_Forged_Stanceless", "stances": [] } ] }
            """;

        /// <summary>Every rule over the npc catalog itself: the words its fields are drawn from, the names
        /// its records are found by, and what a record has to say about itself beyond its shape.</summary>
        [TestMethod]
        public void TheNpcCatalog_OwesNothingToTheChecks()
        {
            CatalogFinding[] owed =
            [
                .. CatalogCrossCheckTests.Shipped.Findings.Where(finding => finding.Catalog == DataCatalog.Npc)
            ];

            Assert.AreEqual(0, owed.Length,
                $"the npc catalog owes the checks:{Environment.NewLine}  {CatalogCrossCheckTests.Lines(owed)}");
        }

        /// <summary>Every species written able to talk has something to say. A gate and not a report: the
        /// claim is written in the npc catalog and the dialogue is written beside it, so the two are either
        /// one decision or a click into silence.</summary>
        [TestMethod]
        public void TalkingSpecies_HaveADialogueInTheCatalog()
        {
            string[] silent =
            [
                .. NarrativeCrossCheckTests.Shipped.Findings
                    .Where(finding => finding.Kind == NarrativeFindingKind.MissingDialogue
                                      && finding.Where.StartsWith(NpcPlace, StringComparison.Ordinal))
                    .Select(finding => finding.Where)
            ];

            Assert.AreEqual(0, silent.Length,
                $"a species is written able to talk with no dialogue behind it:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", silent));
        }

        /// <summary>The mutation the stance rule exists for, put to the same entry point the gate reads: a
        /// record with no stance to roll and none named passes every rule about its SHAPE, and ships an npc
        /// fighting a style nobody chose for it.</summary>
        [TestMethod]
        public void ARecordNamingNoStance_IsFound()
        {
            IReadOnlyList<CatalogFinding> added = CatalogCrossCheckTests.Added(DataCatalog.Npc, StancelessNpcJson);

            Assert.AreEqual(1, added.Count(finding => finding.Kind == CatalogFindingKind.Rule),
                $"a record naming no stance was passed over:{Environment.NewLine}  {CatalogCrossCheckTests.Lines(added)}");
        }
    }
}
