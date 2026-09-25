namespace LastBreathTest.BattleSystemTests
{
    using System.IO;
    using Core.Narrative.Quests;
    using LastBreath.Descriptors;
    using LastBreath.Descriptors.Sandbox;
    using Tooling.Catalogs;

    /// <summary>
    /// The dry run of a dialogue: the game's own dialogue service walked over a world a test types, the
    /// way an author types one into the editor's panel. The rolled entries — the quest offer and the
    /// speech check — are decided by the sandbox's own switch, so a branch behind a roll is read here
    /// rather than left to the engine.
    /// </summary>
    [TestClass]
    public sealed class DialogueDryRunTests
    {
        private const string Veteran = "Npc_Bandit_Veteran";
        private const string FieldOfBones = "Quest_Field_Of_Bones";
        private const string Probe = "Npc_Probe";
        private const string Broken = "Npc_Broken";
        private const string Looping = "Npc_Looping";

        /// <summary>What a sandbox is silent about by nature with no quest seeded into it: the player's
        /// attributes alone. The journal caveat belongs to a world that actually holds a quest.</summary>
        private const int StandingCaveats = 1;

        /// <summary>A dialogue written for these tests, laid out in a data root of its own: the shipped
        /// catalogs answer what the game ships, and a boundary needs a document nobody else reads.</summary>
        private const string ProbeDialogues = """
        {"dialogues":[
         {"npcId":"Npc_Probe","entryRules":[{"priority":0,"conditions":[],"node":"Start"}],
          "nodes":[
           {"id":"Start","lines":[{"speaker":"Npc","key":"Probe_Greeting"}],
            "options":[
              {"id":"Mark","key":"Probe_Mark","oncePerGame":true,
               "actions":[{"type":"SetFact","key":"Probe_Marked"},{"type":"GiveItem","itemId":"Coal","amount":2}],
               "next":"Start"},
              {"id":"Secret","key":"Probe_Secret",
               "visibleConditions":[{"type":"Fact","key":"Probe_Marked"}],"next":"Start"},
              {"id":"Flatter","key":"Probe_Flatter",
               "speechCheck":{"difficulty":2,"failNext":"Bad"},"next":"Good"},
              {"id":"Praise","key":"Probe_Praise","actions":[{"type":"Deed","deedId":"Deed_Flattery"}]},
              {"id":"Trade","key":"Probe_Trade","actions":[{"type":"StartTrade","traderId":"Trader_Ronald"}]},
              {"id":"Leave","key":"Probe_Leave"}]},
           {"id":"Good","lines":[],"options":[{"id":"Back","key":"Probe_Back","next":"Start"}]},
           {"id":"Bad","lines":[],"options":[{"id":"Back","key":"Probe_Back","next":"Start"}]}]},
         {"npcId":"Npc_Looping","entryRules":[{"priority":0,"conditions":[],"node":"Start"}],
          "nodes":[
           {"id":"Start","lines":[],"options":[{"id":"Again","key":"Probe_Again","next":"Start"}]}]},
         {"npcId":"Npc_Broken","entryRules":[{"priority":0,"conditions":[],"node":"Nowhere"}],
          "nodes":[{"id":"Start","lines":[],"options":[{"id":"Leave","key":"Probe_Leave"}]}]}
        ]}
        """;

        /// <summary>The notes are what a run could not READ, and the shipped narrative is read whole: what
        /// a sandbox is silent about by nature stands apart, or a report of what the data owes would be
        /// padded with two lines that are true of every run there will ever be.</summary>
        [TestMethod]
        public void Sandbox_ReadsTheShippedNarrativeWithNothingLeftUnread()
        {
            var sandbox = Shipped(new SandboxWorldState());

            Assert.AreEqual(0, sandbox.Notes.Count, string.Join("; ", sandbox.Notes));
            Assert.AreEqual(StandingCaveats, sandbox.Caveats.Count, string.Join("; ", sandbox.Caveats));
        }

        /// <summary>The journal caveat is about a quest standing on a stage nobody walked into, so it is
        /// owed only where a quest was actually seeded.</summary>
        [TestMethod]
        public void ASeededQuest_IsWhatTheJournalCaveatIsSaidFor()
        {
            var state = new SandboxWorldState();
            state.Quests[FieldOfBones] = QuestStatus.Active;

            NarrativeSandbox seeded = Shipped(state);
            NarrativeSandbox empty = Shipped(new SandboxWorldState());

            Assert.IsTrue(seeded.Caveats.Contains(NarrativeSandbox.SeededQuestNote), string.Join("; ", seeded.Caveats));
            Assert.IsFalse(empty.Caveats.Contains(NarrativeSandbox.SeededQuestNote), string.Join("; ", empty.Caveats));
        }

        /// <summary>A root without the influence catalog falls back to built-in curves, and a chance
        /// shown beside an option has to be one somebody authored.</summary>
        [TestMethod]
        public void ADataRootWithNoInfluenceCurves_SaysSo()
        {
            var sandbox = Probed(new SandboxWorldState());

            Assert.IsTrue(sandbox.Notes.Any(note => note.Contains("built-in defaults", StringComparison.Ordinal)),
                string.Join("; ", sandbox.Notes));
        }

        [TestMethod]
        public void Veteran_WithNoFacts_OffersWorkOnlyWhenTheOfferRollPasses()
        {
            DryRunState passed = Run(Shipped(new SandboxWorldState { Rolls = SandboxRolls.AlwaysPass }), Veteran);
            DryRunState failed = Run(Shipped(new SandboxWorldState { Rolls = SandboxRolls.AlwaysFail }), Veteran);

            Assert.AreEqual("Greeting", passed.NodeId);
            CollectionAssert.AreEqual(new[] { "AskWork", "Flatter", "Leave" }, passed.Options.Select(option => option.Id).ToArray());

            Assert.AreEqual(3, passed.Options.Count(option => option.Visible), "the offer roll passed: work, flattery and leaving");
            Assert.AreEqual(2, failed.Options.Count(option => option.Visible), "the offer roll failed: flattery and leaving alone");
            Assert.IsFalse(failed.Options.Single(option => option.Id == "AskWork").Visible);
        }

        [TestMethod]
        public void Veteran_FlatteryThatFailsItsCheck_LeadsToTheFailNode()
        {
            var run = new DialogueDryRun(Shipped(new SandboxWorldState { Rolls = SandboxRolls.AlwaysFail }));
            run.Start(Veteran);
            run.Choose("Flatter");

            Assert.AreEqual("FlatterBad", run.State.NodeId);
        }

        /// <summary>The same branch on a document these tests own: take <c>failNext</c> out of it and the
        /// conversation ends instead of turning, which is what this holds down.</summary>
        [TestMethod]
        public void ASpeechCheckThatFails_TakesTheFailRoute()
        {
            var failing = new DialogueDryRun(Probed(new SandboxWorldState { Rolls = SandboxRolls.AlwaysFail }));
            failing.Start(Probe);
            failing.Choose("Flatter");

            var passing = new DialogueDryRun(Probed(new SandboxWorldState { Rolls = SandboxRolls.AlwaysPass }));
            passing.Start(Probe);
            passing.Choose("Flatter");

            Assert.AreEqual("Bad", failing.State.NodeId);
            Assert.AreEqual("Good", passing.State.NodeId, "the same option passes its check and takes the other road");
        }

        /// <summary>The chance shown beside the option is the game's curve, whatever the switch does to
        /// the roll itself.</summary>
        [TestMethod]
        public void AnOptionWithASpeechCheck_ShowsWhatTheCheckIsWorth()
        {
            DryRunState run = Run(Shipped(new SandboxWorldState { Rolls = SandboxRolls.AlwaysFail }), Veteran);

            Assert.IsTrue(run.Options.Single(option => option.Id == "Flatter").SpeechCheckChance > 0);
            Assert.IsNull(run.Options.Single(option => option.Id == "Leave").SpeechCheckChance);
        }

        [TestMethod]
        public void Veteran_WithTheQuestActive_OpensTheWorkingNode()
        {
            var state = new SandboxWorldState();
            state.Quests[FieldOfBones] = QuestStatus.Active;

            DryRunState run = Run(Shipped(state), Veteran);

            Assert.AreEqual("Working", run.NodeId, "the entry rule of an active quest outranks the greeting");
            CollectionAssert.AreEqual(new[] { "GiveUp", "Leave" }, run.Options.Select(option => option.Id).ToArray());
            Assert.IsTrue(run.Options.All(option => option is { Visible: true, Enabled: true }));
        }

        [TestMethod]
        public void Veteran_ReadyToTurnIn_ShowsTheTurnInLeadingToThanks()
        {
            var state = new SandboxWorldState();
            state.Quests[FieldOfBones] = QuestStatus.ReadyToTurnIn;

            DryRunState run = Run(Shipped(state), Veteran);

            Assert.AreEqual("HaveIt", run.NodeId);

            DryRunOption turnIn = run.Options.Single(option => option.Id == "TurnIn");
            Assert.IsTrue(turnIn is { Visible: true, Enabled: true }, "nothing is owed that the bag cannot hold");
            Assert.AreEqual("Thanks", turnIn.NextNodeId);
        }

        [TestMethod]
        public void Veteran_TurningIn_CompletesTheQuestInTheSandbox()
        {
            var state = new SandboxWorldState();
            state.Quests[FieldOfBones] = QuestStatus.ReadyToTurnIn;
            state.Items["Coal"] = 3;

            NarrativeSandbox sandbox = Shipped(state);
            var run = new DialogueDryRun(sandbox);
            run.Start(Veteran);
            run.Choose("TurnIn");

            Assert.AreEqual("Thanks", run.State.NodeId);
            Assert.AreEqual(QuestStatus.Completed, sandbox.Quests.Single().Status, "the real quest log ran the turn-in");
            Assert.AreEqual(0, sandbox.Items.GetValueOrDefault("Coal"), "the reward actions took the proof out of the bag");
        }

        [TestMethod]
        public void ChoosingAnOption_RunsItsActionsAgainstTheSandbox()
        {
            NarrativeSandbox sandbox = Probed(new SandboxWorldState());
            var run = new DialogueDryRun(sandbox);
            run.Start(Probe);

            Assert.IsFalse(run.State.Options.Single(option => option.Id == "Secret").Visible,
                "the fact the secret asks after has not been set yet");

            run.Choose("Mark");

            Assert.AreEqual(1, sandbox.Facts.GetValueOrDefault("Probe_Marked"), "SetFact wrote into the sandbox");
            Assert.AreEqual(2, sandbox.Items.GetValueOrDefault("Coal"), "GiveItem filled the sandbox bag");
            Assert.IsTrue(run.State.Options.Single(option => option.Id == "Secret").Visible,
                "the option gated on that fact is offered now");
        }

        [TestMethod]
        public void AnOptionTakenOncePerGame_IsGoneAfterItIsTaken()
        {
            var run = new DialogueDryRun(Probed(new SandboxWorldState()));
            run.Start(Probe);
            run.Choose("Mark");

            Assert.IsFalse(run.State.Options.Single(option => option.Id == "Mark").Visible);
        }

        [TestMethod]
        public void Reset_PutsTheAuthoredWorldBack()
        {
            NarrativeSandbox sandbox = Probed(new SandboxWorldState());
            var run = new DialogueDryRun(sandbox);
            run.Start(Probe);
            run.Choose("Mark");
            run.Reset();

            Assert.AreEqual(0, sandbox.Facts.GetValueOrDefault("Probe_Marked"));
            Assert.AreEqual(0, sandbox.Items.GetValueOrDefault("Coal"));
            Assert.IsTrue(run.State.Options.Single(option => option.Id == "Mark").Visible);
            Assert.AreEqual(0, run.State.Log.Count, "a reset run has done nothing yet");
        }

        [TestMethod]
        public void AnActionOnlyTheGameCanCarryOut_IsWrittenDownInsteadOfRun()
        {
            var run = new DialogueDryRun(Probed(new SandboxWorldState()));
            run.Start(Probe);
            run.Choose("Trade");

            Assert.IsTrue(Refused(run.State, "StartTrade"), "the trade window is named in the log rather than opened");
        }

        /// <summary>A deed moves a standing through the reputation pipeline, and a sandbox states a
        /// relation LEVEL: there is nowhere for it to land, so it must not read as done.</summary>
        [TestMethod]
        public void ADeedADialogueWouldPublish_IsWrittenDownInsteadOfRun()
        {
            var run = new DialogueDryRun(Probed(new SandboxWorldState()));
            run.Start(Probe);
            run.Choose("Praise");

            Assert.IsTrue(Refused(run.State, "Deed"), "a deed nothing in the sandbox can hear must not be logged as done");
        }

        [TestMethod]
        public void AnOptionWithNoNextNode_EndsTheConversation()
        {
            var run = new DialogueDryRun(Probed(new SandboxWorldState()));
            run.Start(Probe);
            run.Choose("Leave");

            Assert.IsFalse(run.State.Running);
            StringAssert.Contains(run.State.Note, "ended", StringComparison.Ordinal);
        }

        [TestMethod]
        public void ADialogueThatWillNotParse_IsSaidRatherThanThrown()
        {
            DryRunState run = Run(Probed(new SandboxWorldState()), Broken);

            Assert.IsFalse(run.Running);
            StringAssert.Contains(run.Note, Broken, StringComparison.Ordinal);
        }

        [TestMethod]
        public void ADialogueThatLoopsForever_StopsAtTheStepLimit()
        {
            var run = new DialogueDryRun(Probed(new SandboxWorldState()));
            run.Start(Looping);

            for (int step = 0; step <= DialogueDryRun.StepLimit; step++)
                run.Choose("Again");

            Assert.IsFalse(run.State.Running);
            StringAssert.Contains(run.State.Note, "circles", StringComparison.Ordinal);
        }

        [TestMethod]
        public void AnNpcNobodyWroteADialogueFor_IsSaidRatherThanThrown()
        {
            DryRunState run = Run(Shipped(new SandboxWorldState()), "Npc_Nobody_Wrote_This");

            Assert.IsFalse(run.Running);
            Assert.AreEqual(0, run.Options.Count);
        }

        /// <summary>Whether the run named an action it would not carry out, rather than reporting it done.</summary>
        private static bool Refused(DryRunState run, string type) =>
            run.Log.Any(line => line.Contains(type, StringComparison.Ordinal)
                                && line.Contains("only the running game can", StringComparison.Ordinal));

        private static DryRunState Run(NarrativeSandbox sandbox, string npcId)
        {
            var run = new DialogueDryRun(sandbox);
            run.Start(npcId);
            return run.State;
        }

        private static NarrativeSandbox Shipped(SandboxWorldState state) =>
            NarrativeSandbox.Load(CatalogWorkspace.Load(SharedData.Root(), CatalogDescriptors.All), state);

        private static NarrativeSandbox Probed(SandboxWorldState state) =>
            NarrativeSandbox.Load(CatalogWorkspace.Load(ProbeRoot(), CatalogDescriptors.All), state);

        /// <summary>A data root holding the tests' own dialogues and nothing else the sandbox reads.
        /// Written once per call: the documents are the input of every case here and none of them
        /// changes a file.</summary>
        private static string ProbeRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "LastBreath", "DialogueDryRunTests");
            Directory.CreateDirectory(Path.Combine(root, "Dialogues"));
            Directory.CreateDirectory(Path.Combine(root, "Quests"));
            File.WriteAllText(Path.Combine(root, "Quests", "Quests.json"), """{"quests":[]}""");
            File.WriteAllText(Path.Combine(root, "Dialogues", "Dialogues.json"), ProbeDialogues);
            return root;
        }
    }
}
