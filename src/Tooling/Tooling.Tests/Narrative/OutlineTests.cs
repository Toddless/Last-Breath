namespace Tooling.Tests.Narrative
{
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Narrative;
    using Tooling.Schema.Model;

    /// <summary>
    /// The outline a narrative editor draws: what a dialogue and a quest look like as the structure
    /// their author works in, and where every row of it is written. The addresses are the point — a row
    /// is how the author reaches a node, an option or a stage, and one wrong pointer would open the
    /// inspector on its neighbour.
    /// </summary>
    [TestClass]
    public class OutlineTests
    {
        private const string DialogueAt = "/dialogues/0";
        private const string QuestAt = "/quests/0";

        private const string Dialogues = """
            {
                "dialogues": [
                    {
                        "npcId": "Npc_Bandit_Veteran",
                        "entryRules": [
                            { "priority": 30, "node": "HaveIt" },
                            { "priority": 0, "node": "Greeting" }
                        ],
                        "nodes": [
                            {
                                "id": "Greeting",
                                "lines": [
                                    { "speaker": "Npc", "key": "Dlg_Veteran_Greeting_1" },
                                    { "speaker": "Player", "key": "Dlg_Veteran_Greeting_2" }
                                ],
                                "options": [
                                    { "id": "AskWork", "key": "Dlg_Veteran_Opt_AskWork", "next": "Offer" },
                                    { "id": "Leave", "key": "Dlg_Opt_Leave" }
                                ]
                            },
                            {
                                "id": "Silent",
                                "options": [ { "key": "" } ]
                            }
                        ]
                    }
                ]
            }
            """;

        private const string Quests = """
            {
                "quests": [
                    {
                        "id": "Quest_Field_Of_Bones",
                        "stages": [
                            {
                                "id": "GatherProof",
                                "objectives": [
                                    { "id": "Proof" },
                                    { "id": "Revenants", "optional": true }
                                ],
                                "transitions": [
                                    { "to": "Report" },
                                    { }
                                ]
                            },
                            {
                                "id": "Report",
                                "outcome": { "id": "Done" }
                            },
                            {
                                "outcome": { "id": "Lost", "fails": true }
                            }
                        ]
                    }
                ]
            }
            """;

        /// <summary>A dialogue with nothing in it at all: the branches still have to be drawn, or a
        /// record the author has only started reads as one the tool could not open.</summary>
        private const string EmptyDialogue = """
            { "dialogues": [ { "npcId": "Npc_Silent" } ] }
            """;

        /// <summary>The options that roll: one whose lost roll leads somewhere, one whose lost roll ends
        /// the conversation, and one that has an id and no wording yet.</summary>
        private const string Rolls = """
            {
                "dialogues": [
                    {
                        "npcId": "Npc_Bandit_Veteran",
                        "nodes": [
                            {
                                "id": "Greeting",
                                "lines": [ { "speaker": "Npc", "key": "Dlg_Veteran_Greeting_1" } ],
                                "options": [
                                    {
                                        "id": "Flatter",
                                        "key": "Dlg_Veteran_Opt_Flatter",
                                        "next": "FlatterGood",
                                        "speechCheck": { "difficulty": 2, "failNext": "FlatterBad" }
                                    },
                                    {
                                        "id": "Bluff",
                                        "key": "Dlg_Veteran_Opt_Bluff",
                                        "next": "Offer",
                                        "speechCheck": { "difficulty": 4 }
                                    },
                                    { "id": "Wait" }
                                ]
                            }
                        ]
                    }
                ]
            }
            """;

        /// <summary>A record whose collections are written as something else: the file is what the author
        /// opened the tool to see, and the reading of it may not answer a mistake with silence.</summary>
        private const string MalformedDialogue = """
            { "dialogues": [ { "npcId": "Npc_Broken", "entryRules": 3, "nodes": {} } ] }
            """;

        private const string MalformedQuest = """
            { "quests": [ { "id": "Quest_Broken", "stages": [ { "id": "Report", "outcome": "Done" } ] } ] }
            """;

        [TestMethod]
        public void Dialogue_IsNamedByTheNpcItBelongsTo_AndHoldsItsTwoBranches()
        {
            OutlineNode dialogue = Outline.Dialogue(DialogueSchema(), Record(Dialogues, DialogueAt), At(DialogueAt));

            Assert.AreEqual("Npc_Bandit_Veteran", dialogue.Label);
            Assert.AreEqual(OutlineKind.Dialogue, dialogue.Kind);
            Assert.AreEqual(At(DialogueAt), dialogue.Pointer);
            Assert.AreEqual(2, dialogue.Children.Count);

            Assert.AreEqual("entry rules   (2)", dialogue.Children[0].Label);
            Assert.AreEqual(At("/dialogues/0/entryRules"), dialogue.Children[0].Pointer);
            Assert.AreEqual(OutlineKind.Group, dialogue.Children[0].Kind);

            Assert.AreEqual("nodes   (2)", dialogue.Children[1].Label);
            Assert.AreEqual(At("/dialogues/0/nodes"), dialogue.Children[1].Pointer);
        }

        [TestMethod]
        public void Dialogue_EntryRule_NamesTheNodeItOpensOnAndItsPriority()
        {
            OutlineNode rule = Outline.Dialogue(DialogueSchema(), Record(Dialogues, DialogueAt), At(DialogueAt))
                .Children[0].Children[0];

            Assert.AreEqual("→ HaveIt   ·   priority 30", rule.Label);
            Assert.AreEqual(OutlineKind.EntryRule, rule.Kind);
            Assert.AreEqual(At("/dialogues/0/entryRules/0"), rule.Pointer);
        }

        [TestMethod]
        public void Dialogue_Node_HoldsItsLinesAndThenItsOptions()
        {
            OutlineNode node = Nodes()[0];

            Assert.AreEqual("Greeting", node.Label);
            Assert.AreEqual(At("/dialogues/0/nodes/0"), node.Pointer);
            Assert.AreEqual(4, node.Children.Count);

            Assert.AreEqual("Npc   ·   Dlg_Veteran_Greeting_1", node.Children[0].Label);
            Assert.AreEqual(OutlineKind.Line, node.Children[0].Kind);
            Assert.AreEqual(At("/dialogues/0/nodes/0/lines/0"), node.Children[0].Pointer);
            Assert.AreEqual("Player   ·   Dlg_Veteran_Greeting_2", node.Children[1].Label);

            Assert.AreEqual(OutlineKind.Option, node.Children[2].Kind);
            Assert.AreEqual(At("/dialogues/0/nodes/0/options/0"), node.Children[2].Pointer);
            Assert.AreEqual(At("/dialogues/0/nodes/0/options/1"), node.Children[3].Pointer);
        }

        /// <summary>The key is the row's text as far as the author is concerned, and the host reads the
        /// translation under it; a row that only showed the translation would be a row nobody can search
        /// the .po for.</summary>
        [TestMethod]
        public void Dialogue_LineAndOption_CarryTheKeyTheirTextIsReadUnder()
        {
            OutlineNode node = Nodes()[0];

            Assert.AreEqual("Dlg_Veteran_Greeting_1", node.Children[0].Key);
            Assert.AreEqual("Dlg_Veteran_Opt_AskWork", node.Children[2].Key);
        }

        [TestMethod]
        public void Dialogue_Option_NamesWhereItLeads()
        {
            Assert.AreEqual("Dlg_Veteran_Opt_AskWork   → Offer", Nodes()[0].Children[2].Label);
        }

        /// <summary>An option leading nowhere ends the conversation, which is a decision and not an
        /// omission: the row says so instead of trailing off.</summary>
        [TestMethod]
        public void Dialogue_OptionWithoutNext_SaysTheConversationEnds()
        {
            Assert.AreEqual("Dlg_Opt_Leave   ·   ends", Nodes()[0].Children[3].Label);
        }

        [TestMethod]
        public void Dialogue_NodeWithoutLines_SaysSoAndKeepsItsOptions()
        {
            OutlineNode node = Nodes()[1];

            Assert.AreEqual("Silent   ·   no lines", node.Label);
            Assert.AreEqual(1, node.Children.Count);
            Assert.AreEqual(At("/dialogues/0/nodes/1/options/0"), node.Children[0].Pointer);
        }

        /// <summary>A key written as nothing is a key nobody has typed yet, and the row has to be
        /// readable while it is: the option is named by its place, the way every element carrying no name
        /// of its own is.</summary>
        [TestMethod]
        public void Dialogue_OptionWithoutAKey_IsNamedByItsPlace()
        {
            Assert.AreEqual("#0   ·   ends", Nodes()[1].Children[0].Label);
        }

        /// <summary>An option is named by the id its node's routes point at while its wording has not been
        /// written: the author is reading which choice this is, and the key is only where its text lives.</summary>
        [TestMethod]
        public void Dialogue_OptionWithoutAKey_IsNamedByItsId()
        {
            Assert.AreEqual("Wait   ·   ends", Options(Rolls)[2].Label);
        }

        /// <summary>An option that rolls has two routes out of it, and the row names both: a row showing
        /// only the won one hides half the conversation.</summary>
        [TestMethod]
        public void Dialogue_OptionThatRolls_NamesBothRoutes()
        {
            Assert.AreEqual("Dlg_Veteran_Opt_Flatter   → FlatterGood   ·   fail → FlatterBad", Options(Rolls)[0].Label);
        }

        [TestMethod]
        public void Dialogue_OptionWhoseRollLeadsNowhere_SaysTheConversationEnds()
        {
            Assert.AreEqual("Dlg_Veteran_Opt_Bluff   → Offer   ·   fail ends", Options(Rolls)[1].Label);
        }

        /// <summary>A collection written as something that is not one draws no rows, and neither does a
        /// key nobody wrote: the heading is the only place the two can be told apart.</summary>
        [TestMethod]
        public void Dialogue_CollectionWrittenAsSomethingElse_SaysSoInsteadOfCountingNothing()
        {
            OutlineNode dialogue =
                Outline.Dialogue(DialogueSchema(), Record(MalformedDialogue, DialogueAt), At(DialogueAt));

            Assert.AreEqual("entry rules   (not a list)", dialogue.Children[0].Label);
            Assert.AreEqual(0, dialogue.Children[0].Children.Count);

            Assert.AreEqual("nodes   (not a list)", dialogue.Children[1].Label);
            Assert.AreEqual(0, dialogue.Children[1].Children.Count);
        }

        [TestMethod]
        public void Dialogue_WithoutRulesOrNodes_StillDrawsBothBranches()
        {
            OutlineNode dialogue =
                Outline.Dialogue(DialogueSchema(), Record(EmptyDialogue, DialogueAt), At(DialogueAt));

            Assert.AreEqual("entry rules   (0)", dialogue.Children[0].Label);
            Assert.AreEqual("nodes   (0)", dialogue.Children[1].Label);
            Assert.AreEqual(0, dialogue.Children[1].Children.Count);
        }

        /// <summary>Every row an inspector may be opened on carries the schema of what stands there; a
        /// branch is a collection and carries none, which is what tells the two apart.</summary>
        [TestMethod]
        public void Dialogue_EveryRowCarriesTheSchemaOfItsOwnAddress()
        {
            OutlineNode dialogue = Outline.Dialogue(DialogueSchema(), Record(Dialogues, DialogueAt), At(DialogueAt));
            OutlineNode node = dialogue.Children[1].Children[0];

            Assert.AreEqual("DialogueEntry", dialogue.Schema?.TypeName);
            Assert.IsNull(dialogue.Children[1].Schema);
            Assert.AreEqual("DialogueNodeEntry", node.Schema?.TypeName);
            Assert.AreEqual("DialogueLineEntry", node.Children[0].Schema?.TypeName);
            Assert.AreEqual("DialogueOptionEntry", node.Children[2].Schema?.TypeName);
        }

        /// <summary>A collection this build has no schema for is still the author's data: the rows are
        /// drawn and simply answer for nothing an inspector could draw.</summary>
        [TestMethod]
        public void Dialogue_CollectionTheSchemaDoesNotDescribe_IsStillDrawn()
        {
            RecordSchema undescribed = new() { TypeName = "DialogueEntry", Fields = [], IdField = "npcId" };

            OutlineNode dialogue = Outline.Dialogue(undescribed, Record(Dialogues, DialogueAt), At(DialogueAt));

            Assert.AreEqual("nodes   (2)", dialogue.Children[1].Label);
            Assert.IsNull(dialogue.Children[1].Children[0].Schema);
            Assert.AreEqual("Greeting", dialogue.Children[1].Children[0].Label);
        }

        [TestMethod]
        public void Quest_IsNamedByItsIdAndHoldsItsStages()
        {
            OutlineNode quest = Outline.Quest(QuestSchema(), Record(Quests, QuestAt), At(QuestAt));

            Assert.AreEqual("Quest_Field_Of_Bones", quest.Label);
            Assert.AreEqual(OutlineKind.Quest, quest.Kind);
            Assert.AreEqual(1, quest.Children.Count);
            Assert.AreEqual("stages   (3)", quest.Children[0].Label);
            Assert.AreEqual(At("/quests/0/stages"), quest.Children[0].Pointer);
        }

        [TestMethod]
        public void Quest_Stage_HoldsItsObjectivesAndThenItsRoutes()
        {
            OutlineNode stage = Stages()[0];

            Assert.AreEqual("GatherProof", stage.Label);
            Assert.AreEqual(OutlineKind.Stage, stage.Kind);
            Assert.AreEqual(4, stage.Children.Count);

            Assert.AreEqual("objective   ·   Proof", stage.Children[0].Label);
            Assert.AreEqual(OutlineKind.Objective, stage.Children[0].Kind);
            Assert.AreEqual(At("/quests/0/stages/0/objectives/0"), stage.Children[0].Pointer);

            Assert.AreEqual("objective   ·   Revenants   ·   optional", stage.Children[1].Label);

            Assert.AreEqual("→ Report", stage.Children[2].Label);
            Assert.AreEqual(OutlineKind.Transition, stage.Children[2].Kind);
            Assert.AreEqual(At("/quests/0/stages/0/transitions/0"), stage.Children[2].Pointer);
        }

        /// <summary>A route naming no stage leads nowhere the game can follow, and the row has to show
        /// the hole rather than stand blank.</summary>
        [TestMethod]
        public void Quest_TransitionWithoutATarget_IsStillARow()
        {
            Assert.AreEqual("→ —", Stages()[0].Children[3].Label);
        }

        [TestMethod]
        public void Quest_Outcome_StandsUnderTheStageThatEndsThere()
        {
            OutlineNode outcome = Stages()[1].Children[0];

            Assert.AreEqual("outcome   ·   Done", outcome.Label);
            Assert.AreEqual(OutlineKind.Outcome, outcome.Kind);
            Assert.AreEqual(At("/quests/0/stages/1/outcome"), outcome.Pointer);
            Assert.AreEqual("QuestOutcomeEntry", outcome.Schema?.TypeName);
        }

        [TestMethod]
        public void Quest_OutcomeThatFails_SaysSo()
        {
            Assert.AreEqual("outcome   ·   Lost   ·   fails", Stages()[2].Children[0].Label);
        }

        /// <summary>An ending written as something other than a record is still a row: the stage ends
        /// there as far as its author is concerned, and a row simply not drawn would leave him reading a
        /// stage that goes nowhere.</summary>
        [TestMethod]
        public void Quest_OutcomeWrittenAsSomethingElse_IsStillARow()
        {
            OutlineNode outcome = Outline.Quest(QuestSchema(), Record(MalformedQuest, QuestAt), At(QuestAt))
                .Children[0].Children[0].Children[0];

            Assert.AreEqual("outcome   ·   not a record", outcome.Label);
            Assert.AreEqual(OutlineKind.Outcome, outcome.Kind);
            Assert.AreEqual(At("/quests/0/stages/0/outcome"), outcome.Pointer);
        }

        /// <summary>A stage that carries no name of its own is named by its place, so the row can still
        /// be pointed at while the author is writing it.</summary>
        [TestMethod]
        public void Quest_StageWithoutAnId_IsNamedByItsPlace()
        {
            Assert.AreEqual("#2", Stages()[2].Label);
        }

        private static JsonPointer At(string text) => JsonPointer.Parse(text);

        private static JToken Record(string json, string at) => At(at).Resolve(JToken.Parse(json))!;

        private static IReadOnlyList<OutlineNode> Nodes() => Nodes(Dialogues);

        private static IReadOnlyList<OutlineNode> Nodes(string json) =>
            Outline.Dialogue(DialogueSchema(), Record(json, DialogueAt), At(DialogueAt)).Children[1].Children;

        /// <summary>The options of the first node, which stand under it after its one line.</summary>
        private static IReadOnlyList<OutlineNode> Options(string json) =>
            [.. Nodes(json)[0].Children.Where(row => row.Kind == OutlineKind.Option)];

        private static IReadOnlyList<OutlineNode> Stages() =>
            Outline.Quest(QuestSchema(), Record(Quests, QuestAt), At(QuestAt)).Children[0].Children;

        /// <summary>The dialogue record as the game's own descriptor builds it, written out by hand:
        /// the tests hold the reading of a document and not the reflector that produced the schema.</summary>
        private static RecordSchema DialogueSchema()
        {
            RecordSchema line = Shape("DialogueLineEntry", Word("speaker"), Word("key"));
            RecordSchema option = Shape("DialogueOptionEntry", Word("id"), Word("key"), Word("next"));
            RecordSchema node = Shape("DialogueNodeEntry", Word("id"), List("lines", line), List("options", option));
            RecordSchema rule = Shape("DialogueEntryRuleEntry", Word("node"), Word("priority"));

            return Shape("DialogueEntry", Word("npcId"), List("entryRules", rule), List("nodes", node)) with
            {
                IdField = "npcId"
            };
        }

        private static RecordSchema QuestSchema()
        {
            RecordSchema objective = Shape("QuestObjectiveEntry", Word("id"), Word("optional"));
            RecordSchema transition = Shape("QuestTransitionEntry", Word("to"));
            RecordSchema outcome = Shape("QuestOutcomeEntry", Word("id"), Word("fails"));

            RecordSchema stage = Shape(
                "QuestStageEntry",
                Word("id"),
                List("objectives", objective),
                List("transitions", transition),
                Nested("outcome", outcome));

            return Shape("QuestEntry", Word("id"), List("stages", stage)) with { IdField = "id" };
        }

        private static RecordSchema Shape(string type, params FieldSchema[] fields) =>
            new() { TypeName = type, Fields = fields };

        private static FieldSchema Word(string name) => new() { JsonName = name, Kind = FieldKind.String };

        private static FieldSchema Nested(string name, RecordSchema record) =>
            new() { JsonName = name, Kind = FieldKind.Object, Record = record };

        private static FieldSchema List(string name, RecordSchema element) =>
            new()
            {
                JsonName = name,
                Kind = FieldKind.Array,
                Item = new FieldSchema { JsonName = FieldSchema.Unnamed, Kind = FieldKind.Object, Record = element }
            };
    }
}
