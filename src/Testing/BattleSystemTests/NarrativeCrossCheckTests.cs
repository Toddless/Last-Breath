namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.DialogueData;
    using Core.Data.QuestData;
    using Core.Narrative.Validation;
    using LastBreath.Descriptors;
    using Newtonsoft.Json;
    using Tooling.Catalogs;
    using Tooling.Localization;

    /// <summary>
    /// The shipped narrative held against everything outside it: the ids it names, the dialogues its
    /// quests are taken from, the routes inside a conversation and the wording every line is read under.
    /// The rules are the game's own and the authoring tool runs the very same ones, so what an author
    /// sees in the tool's Checks panel and what this run reports are one answer.
    /// <para>A report and not a gate: what the data owes today is PINNED, so a finding arriving fails and
    /// a finding going away fails just as loudly — the second is how a pin outlives the thing it pinned.</para>
    /// </summary>
    [TestClass]
    public class NarrativeCrossCheckTests
    {
        /// <summary>The folder of .po files, which sits among the data catalogs and is read like one.</summary>
        private const string LocalizationFolder = "Localization";

        /// <summary>The trader whose day the whole villager foundation is written around: his dialogue
        /// opens a shop and hands out the trials, so an action registry short of one word drops it whole.</summary>
        private const string TraderNpcId = "Npc_Ronald";

        private const string VeteranNpcId = "Npc_Bandit_Veteran";

        /// <summary>The four trials, which are what spawns npcs, pays in tree points and hands out the
        /// ornaments. Named one by one: a quest dropped at parse is a quest this run would count as
        /// written and never see the inside of.</summary>
        private static readonly string[] s_trials =
        [
            "Quest_Trial_Of_The_Fang",
            "Quest_Trial_Of_The_Pack",
            "Quest_Trial_Of_The_Mountain",
            "Quest_Trial_Of_The_Grave",
        ];

        /// <summary>
        /// What the shipped narrative owes today, one row per place. Held as (kind, where) pairs and
        /// nothing else: the wording of a message is the tool's to improve, while the sort of thing and
        /// the place it is in are the fact.
        /// </summary>
        /// <remarks>A row is taken out when the data is fixed, never to quiet a run. The first two are the
        /// tool's own gap and not the author's: no descriptor is written for those catalogs
        /// (<c>CatalogDescriptors.NotYetDescribed</c>), so every item and npc-modifier id the narrative
        /// names is left unanswered rather than called broken. Describing them turns these two rows into
        /// real answers with nothing else to change — as describing the traders and the deeds already
        /// has: the shop a dialogue opens and the deed it publishes are now answered, not passed over.
        /// <para>The last two are the author's, and owed rather than broken: the game counts kills per
        /// faction and remembers who has been spoken to, and no quest and no dialogue asks about either
        /// yet. Both are families a narrative would read the day one is written around them.</para></remarks>
        private static readonly (NarrativeFindingKind Kind, string Where)[] s_known =
        [
            (NarrativeFindingKind.UndescribedTarget, "Items"),
            (NarrativeFindingKind.UndescribedTarget, "NpcModifiers"),
            (NarrativeFindingKind.FactNeverRead, "facts/Kill_Count_Faction:<faction>"),
            (NarrativeFindingKind.FactNeverRead, "facts/Npc_Talked:<npcId>"),
        ];

        private static NarrativeCheckReport s_report = null!;

        [ClassInitialize]
        public static void Check(TestContext context) => s_report = Run();

        /// <summary>One run of the checks over the shipped data, through the same entry point the
        /// authoring tool presses its button on.</summary>
        internal static NarrativeCheckReport Run()
        {
            string root = SharedData.Root();
            var workspace = CatalogWorkspace.Load(root, CatalogDescriptors.All);

            return NarrativeCheckRun.Over(
                workspace, new ReferenceIndex(workspace), LocalizedTexts.Load(Path.Combine(root, LocalizationFolder)));
        }

        [TestMethod]
        public void TheShippedNarrativeHoldsOnlyTheKnownFindings()
        {
            (NarrativeFindingKind Kind, string Where)[] found =
                [.. s_report.Findings.Select(finding => (finding.Kind, finding.Where)).Distinct().Order()];

            Report("What the checks found in the shipped narrative", [.. s_report.Findings.Select(Line)]);
            Report("What the run could not read", [.. s_report.Notes]);

            CollectionAssert.AreEquivalent(
                s_known,
                found,
                $"the shipped narrative holds other findings than the known ones:{Environment.NewLine}  "
                + string.Join($"{Environment.NewLine}  ", s_report.Findings.Select(Line)));
        }

        /// <summary>The run has to be reading something: a workspace that opened no narrative would find
        /// nothing to say and pass forever.</summary>
        [TestMethod]
        public void TheRunReadsTheNarrativeCatalogsAtAll()
        {
            Assert.AreNotEqual(0, s_report.Read.Dialogues.Count, "the run opened no dialogue at all");
            Assert.AreNotEqual(0, s_report.Read.Quests.Count, "the run opened no quest at all");
            Assert.AreNotEqual(0, s_report.Read.Texts.Locales.Count, "the run read no locale, so no line was held to the wording");
        }

        /// <summary>
        /// The records the vocabulary's own gaps used to swallow. The parse is the game's, run over the
        /// shipped documents: a registry short of one action drops the whole record naming it, and the
        /// trader and his four trials are where every such word is written.
        /// </summary>
        [TestMethod]
        public void TheTraderAndTheFourTrials_ParseWhole()
        {
            string[] dropped =
            [
                .. s_report.Findings
                    .Where(finding => finding.Kind == NarrativeFindingKind.Dropped)
                    .Select(finding => finding.Where)
            ];

            Assert.AreEqual(0, dropped.Length, $"the loader kept none of: {string.Join(", ", dropped)}");

            NarrativeCheckInput input = s_report.Read;

            CollectionAssert.Contains(input.LoadedDialogues.ToArray(), TraderNpcId,
                $"the dialogue of '{TraderNpcId}' was dropped at parse");
            CollectionAssert.Contains(input.LoadedDialogues.ToArray(), VeteranNpcId,
                $"the dialogue of '{VeteranNpcId}' was dropped at parse");

            foreach (string trial in s_trials)
                CollectionAssert.Contains(input.LoadedQuests.ToArray(), trial, $"'{trial}' was dropped at parse");
        }

        /// <summary>The mutation the whole run exists for: an option routed to a node nobody wrote is what
        /// the loader drops the WHOLE dialogue over, leaving one npc silent and a line in the log. The
        /// checks have to name the option, not the file.</summary>
        [TestMethod]
        public void AnOptionRoutedToANodeNobodyWrote_IsFound()
        {
            IReadOnlyList<NarrativeFinding> findings = Forged(ForgedDialogueJson, ForgedQuestJson);

            Assert.AreEqual(1, findings.Count(finding => finding.Kind == NarrativeFindingKind.DanglingNode),
                $"a route leading nowhere went unnoticed:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", findings.Select(Line))}");
            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.DanglingNode
                                                  && finding.Where.Contains("Leave", StringComparison.Ordinal)),
                "the finding names the file rather than the option the author has to open");
        }

        /// <summary>The rest of what one forged pair of records owes, each rule against a case of its own:
        /// a node nothing routes to, a stage nothing answers, an npc with no dialogue, an id nothing
        /// writes, a word no factory reads and a line in no locale.</summary>
        [TestMethod]
        public void TheForgedRecords_AreFoundOneRuleAtATime()
        {
            IReadOnlyList<NarrativeFinding> findings = Forged(ForgedDialogueJson, ForgedQuestJson);

            NarrativeFindingKind[] expected =
            [
                NarrativeFindingKind.DanglingNode,
                NarrativeFindingKind.UnreachableNode,
                NarrativeFindingKind.DanglingStage,
                NarrativeFindingKind.MissingDialogue,
                NarrativeFindingKind.UnknownReference,
                NarrativeFindingKind.UnknownEntry,
                NarrativeFindingKind.MissingText,
            ];

            foreach (NarrativeFindingKind kind in expected)
                Assert.IsTrue(findings.Any(finding => finding.Kind == kind),
                    $"nothing of kind {kind} was found in the forged records:{Environment.NewLine}  "
                    + string.Join($"{Environment.NewLine}  ", findings.Select(Line)));
        }

        /// <summary>The boundaries a report must not swallow: a dialogue nothing opens, a quest with no
        /// stages, and a run that read no wording at all.</summary>
        [TestMethod]
        public void TheEmptyRecords_AreNotPassedOver()
        {
            IReadOnlyList<NarrativeFinding> findings = NarrativeChecks.Run(new NarrativeCheckInput
            {
                Dialogues = Records<DialoguesData>(EmptyDialogueJson).Dialogues,
                Quests = Records<QuestsData>(EmptyQuestJson).Quests,
                LoadedDialogues = [],
                LoadedQuests = [],
                Ids = new NarrativeIdSource(_ => true, (_, _) => true),
                Texts = new NarrativeTextSource(LocalizedTexts.ReferenceLocale, [], (_, _) => false)
            });

            Assert.AreEqual(1, findings.Count(finding => finding.Kind == NarrativeFindingKind.Incomplete
                                                         && finding.Where == "localization"),
                "a run that read no locale at all said nothing about it");

            string[] incomplete = [.. findings.Where(finding => finding.Kind == NarrativeFindingKind.Incomplete).Select(finding => finding.Message)];

            Assert.IsTrue(incomplete.Any(message => message.Contains("entry rule", StringComparison.Ordinal)),
                "a dialogue nothing opens was passed over");
            Assert.IsTrue(incomplete.Any(message => message.Contains("no stage", StringComparison.Ordinal)),
                "a quest with no stages was passed over");
        }

        /// <summary>One forged pair of records run through the rules with everything outside them
        /// answering: what is found is the records' own doing and not the bench's.</summary>
        private static IReadOnlyList<NarrativeFinding> Forged(string dialogues, string quests)
        {
            var dialogue = Records<DialoguesData>(dialogues);
            var quest = Records<QuestsData>(quests);

            return NarrativeChecks.Run(new NarrativeCheckInput
            {
                Dialogues = dialogue.Dialogues,
                Quests = quest.Quests,
                LoadedDialogues = [.. dialogue.Dialogues.Select(entry => entry.NpcId)],
                LoadedQuests = [.. quest.Quests.Select(entry => entry.Id)],
                Ids = new NarrativeIdSource(_ => true, (_, id) => id.StartsWith("Npc_", StringComparison.Ordinal)),
                Texts = new NarrativeTextSource(
                    LocalizedTexts.ReferenceLocale,
                    [LocalizedTexts.ReferenceLocale],
                    (_, key) => !key.EndsWith("_Unwritten", StringComparison.Ordinal))
            });
        }

        private static T Records<T>(string json) => JsonConvert.DeserializeObject<T>(json)!;

        private static string Line(NarrativeFinding finding) => $"{finding.Kind}  {finding.Where}  —  {finding.Message}";

        private static void Report(string what, IReadOnlyList<string> lines) =>
            Console.WriteLine(lines.Count == 0
                ? $"{what}: none."
                : $"{what} ({lines.Count}):{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", lines)}");

        /// <summary>A dialogue whose farewell leads to a node nobody wrote, whose second node nothing
        /// routes to, and whose greeting is read under a key no locale has.</summary>
        private const string ForgedDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dialogue_Forged_Greeting_Unwritten" } ],
                      "options": [
                        { "id": "Leave", "key": "Dialogue_Forged_Leave", "next": "Nowhere" }
                      ]
                    },
                    {
                      "id": "Orphan",
                      "lines": [],
                      "options": [ { "id": "Back", "key": "Dialogue_Forged_Back" } ]
                    }
                  ]
                }
              ]
            }
            """;

        /// <summary>A quest taken from an npc who never speaks, routed to a stage nobody wrote, rewarding
        /// an item no catalog holds, and gated on a word no factory reads.</summary>
        private const string ForgedQuestJson =
            """
            {
              "quests": [
                {
                  "id": "Quest_Forged",
                  "giverNpcId": "Npc_Silent",
                  "turnInNpcIds": [ "Npc_Silent" ],
                  "acceptConditions": [ { "type": "NoSuchCondition" } ],
                  "rewards": { "items": [ { "itemId": "Item_Nobody_Wrote", "amount": 1 } ] },
                  "stages": [
                    {
                      "id": "Start",
                      "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Forged", "amount": 1 } } ],
                      "transitions": [ { "to": "Elsewhere" } ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string EmptyDialogueJson =
            """
            { "dialogues": [ { "npcId": "Npc_Empty", "nodes": [], "entryRules": [] } ] }
            """;

        private const string EmptyQuestJson =
            """
            { "quests": [ { "id": "Quest_Empty", "giverNpcId": "Npc_Empty", "stages": [] } ] }
            """;

        /// <summary>A key written as null is a key the loader takes and the reader cannot: the rules read
        /// the record's strings straight, and a run that dies on one leaves the author with a tool that
        /// stopped rather than a line to fix.</summary>
        [TestMethod]
        public void ALineWrittenWithANullKey_IsSaidRatherThanThrown()
        {
            IReadOnlyList<NarrativeFinding> findings = Read(NullKeyDialogueJson, NoQuestsJson);

            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.Incomplete
                                                  && finding.Message.Contains("'key'", StringComparison.Ordinal)),
                Lines(findings));
        }

        /// <summary>Every word the game parses into a member of a named set, held to that set: the parsers
        /// throw on anything else, which takes the whole record out of the game without naming the word.</summary>
        [TestMethod]
        public void AWordOutsideTheMembersOffered_IsFound()
        {
            IReadOnlyList<NarrativeFinding> findings = Read(BadMemberDialogueJson, BadMemberQuestJson);

            string[] said =
            [
                .. findings.Where(finding => finding.Kind == NarrativeFindingKind.UnknownChoice).Select(finding => finding.Where)
            ];

            Assert.AreEqual(4, said.Length, Lines(findings));

            foreach (string key in new[] { "speaker", "faction", "declinePolicy", "status" })
                Assert.IsTrue(said.Any(where => where.EndsWith(key, StringComparison.Ordinal)),
                    $"'{key}' was passed over:{Environment.NewLine}  {Lines(findings)}");
        }

        /// <summary>A list of conditions written as one object gates nothing: the parsers read no entry out
        /// of it, so the clause the author believes he wrote is not there at all.</summary>
        [TestMethod]
        public void ConditionsWrittenAsAnObject_AreNotPassedOver()
        {
            IReadOnlyList<NarrativeFinding> findings = Read(ObjectConditionsDialogueJson, NoQuestsJson);

            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.UnknownEntry
                                                  && finding.Message.Contains("rather than as an array", StringComparison.Ordinal)),
                Lines(findings));
        }

        /// <summary>The word inside a composite is read by the same rule as the word outside it — a run
        /// that walked only the top of an AnyOf would pass a clause no factory reads.</summary>
        [TestMethod]
        public void AConditionNestedInACompositeUnderAnUnknownWord_IsFound()
        {
            IReadOnlyList<NarrativeFinding> findings = Read(NoDialoguesJson, NestedConditionQuestJson);

            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.UnknownEntry
                                                  && finding.Where.Contains("acceptConditions[0]/conditions[0]", StringComparison.Ordinal)),
                Lines(findings));
        }

        /// <summary>Two records written under one id leave the loader holding one of them, and everything
        /// the other says out of the game with nothing said about it.</summary>
        [TestMethod]
        public void TwoRecordsWrittenUnderOneId_AreFound()
        {
            IReadOnlyList<NarrativeFinding> findings = Read(TwinDialogueJson, TwinQuestJson);

            string[] duplicates =
            [
                .. findings.Where(finding => finding.Kind == NarrativeFindingKind.DuplicateId).Select(finding => finding.Where)
            ];

            CollectionAssert.AreEquivalent(new[] { "Dialogues/Npc_Twin", "Quests/Quest_Twin" }, duplicates, Lines(findings));
        }

        /// <summary>The three shapes the quest loader refuses a whole quest over. Each of them used to
        /// arrive as a bare drop, which says a quest is gone and not one word about why.</summary>
        [TestMethod]
        public void TheQuestShapesTheLoaderRefuses_AreFoundOneAtATime()
        {
            IReadOnlyList<NarrativeFinding> findings = Read(NoDialoguesJson, RefusedQuestJson);

            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.Incomplete
                                                  && finding.Where == "Quests/Quest_Ending_With_Routes/stages/Only"
                                                  && finding.Message.Contains("an ending leads nowhere", StringComparison.Ordinal)),
                Lines(findings));

            Assert.IsTrue(findings.Any(finding => finding.Kind == NarrativeFindingKind.Incomplete
                                                  && finding.Where == "Quests/Quest_Unloseable/stages/Only/outcome/fails"),
                Lines(findings));

            string[] looping =
            [
                .. findings.Where(finding => finding.Kind == NarrativeFindingKind.LoopingStage).Select(finding => finding.Where)
            ];

            CollectionAssert.AreEquivalent(
                new[] { "Quests/Quest_Ring/stages/First", "Quests/Quest_Ring/stages/Second" }, looping, Lines(findings));
        }

        /// <summary>One forged pair of documents run through the rules with every id and every key
        /// answered: what is found is the documents' own doing and not the bench's.</summary>
        private static IReadOnlyList<NarrativeFinding> Read(string dialogues, string quests)
        {
            var dialogue = Records<DialoguesData>(dialogues);
            var quest = Records<QuestsData>(quests);

            return NarrativeChecks.Run(new NarrativeCheckInput
            {
                Dialogues = dialogue.Dialogues,
                Quests = quest.Quests,
                LoadedDialogues = [.. dialogue.Dialogues.Select(entry => entry.NpcId)],
                LoadedQuests = [.. quest.Quests.Select(entry => entry.Id)],
                Ids = new NarrativeIdSource(_ => true, (_, _) => true),
                Texts = new NarrativeTextSource(
                    LocalizedTexts.ReferenceLocale, [LocalizedTexts.ReferenceLocale], (_, _) => true)
            });
        }

        private static string Lines(IReadOnlyList<NarrativeFinding> findings) =>
            string.Join($"{Environment.NewLine}  ", findings.Select(Line));

        private const string NoDialoguesJson = """{ "dialogues": [] }""";

        private const string NoQuestsJson = """{ "quests": [] }""";

        private const string NullKeyDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": null } ],
                      "options": [ { "id": "Leave", "key": "Dialogue_Forged_Leave" } ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string BadMemberDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Shouter", "key": "Dialogue_Forged_Greeting" } ],
                      "options": [ { "id": "Leave", "key": "Dialogue_Forged_Leave" } ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string BadMemberQuestJson =
            """
            {
              "quests": [
                {
                  "id": "Quest_Forged",
                  "giverNpcId": "Npc_Forged",
                  "faction": "Halfling",
                  "declinePolicy": "Whenever",
                  "acceptConditions": [ { "type": "QuestStatus", "questId": "Quest_Forged", "status": "Elsewhere" } ],
                  "stages": [
                    {
                      "id": "Only",
                      "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Forged", "amount": 1 } } ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string ObjectConditionsDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Forged",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [ { "speaker": "Npc", "key": "Dialogue_Forged_Greeting" } ],
                      "options": [
                        {
                          "id": "Leave",
                          "key": "Dialogue_Forged_Leave",
                          "visibleConditions": { "type": "Fact", "key": "Fact_Forged" }
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string NestedConditionQuestJson =
            """
            {
              "quests": [
                {
                  "id": "Quest_Forged",
                  "giverNpcId": "Npc_Forged",
                  "acceptConditions": [
                    { "type": "AnyOf", "conditions": [ { "type": "NoSuchInnerCondition" } ] }
                  ],
                  "stages": [
                    {
                      "id": "Only",
                      "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Forged", "amount": 1 } } ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string TwinDialogueJson =
            """
            {
              "dialogues": [
                {
                  "npcId": "Npc_Twin",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [],
                      "options": [ { "id": "Leave", "key": "Dialogue_Twin_Leave" } ]
                    }
                  ]
                },
                {
                  "npcId": "Npc_Twin",
                  "entryRules": [ { "priority": 0, "node": "Greeting" } ],
                  "nodes": [
                    {
                      "id": "Greeting",
                      "lines": [],
                      "options": [ { "id": "Leave", "key": "Dialogue_Twin_Leave" } ]
                    }
                  ]
                }
              ]
            }
            """;

        private const string TwinQuestJson =
            """
            {
              "quests": [
                {
                  "id": "Quest_Twin",
                  "giverNpcId": "Npc_Twin",
                  "stages": [
                    { "id": "Only", "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Twin", "amount": 1 } } ] }
                  ]
                },
                {
                  "id": "Quest_Twin",
                  "giverNpcId": "Npc_Twin",
                  "stages": [
                    { "id": "Only", "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Twin", "amount": 1 } } ] }
                  ]
                }
              ]
            }
            """;

        /// <summary>One quest per way of writing a stage graph the loader refuses: an ending that also
        /// declares routes, an ending that buries a quest written unable to fail, and two stages routing
        /// into each other.</summary>
        private const string RefusedQuestJson =
            """
            {
              "quests": [
                {
                  "id": "Quest_Ending_With_Routes",
                  "giverNpcId": "Npc_Forged",
                  "stages": [
                    {
                      "id": "Only",
                      "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Forged", "amount": 1 } } ],
                      "transitions": [ { "to": "Only" } ],
                      "outcome": { "id": "Done" }
                    }
                  ]
                },
                {
                  "id": "Quest_Unloseable",
                  "giverNpcId": "Npc_Forged",
                  "canFail": false,
                  "stages": [
                    {
                      "id": "Only",
                      "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Forged", "amount": 1 } } ],
                      "outcome": { "id": "Lost", "fails": true }
                    }
                  ]
                },
                {
                  "id": "Quest_Ring",
                  "giverNpcId": "Npc_Forged",
                  "stages": [
                    {
                      "id": "First",
                      "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Forged", "amount": 1 } } ],
                      "transitions": [ { "to": "Second" } ]
                    },
                    {
                      "id": "Second",
                      "objectives": [ { "id": "Talk", "counter": { "key": "Fact_Forged", "amount": 1 } } ],
                      "transitions": [ { "to": "First" } ]
                    }
                  ]
                }
              ]
            }
            """;
    }
}
