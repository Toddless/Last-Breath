namespace LastBreathTest.Narrative
{
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Reputation;
    using Core.Save;
    using Core.Services;
    using LastBreath.Descriptors;
    using Moq;

    /// <summary>
    /// The quest and dialogue parsers over the full registered vocabulary, fed records written here, and the
    /// dialogue service over what they read. The generator is strict: an offer roll is read from the facts.
    /// </summary>
    [TestClass]
    public class NarrativeDataParseTests
    {
        private const string FixtureFile = "fixture.json";

        /// <summary>A file whose list is written as null: the loader is handed no list at all there.</summary>
        private const string EmptiedFile = "emptied.json";

        private const string NullDialoguesJson = """{"dialogues": null}""";

        private const string NullQuestsJson = """{"quests": null}""";

        private const string QuestId = "Quest_Fixture_Errand";

        private const string GiverNpcId = "Npc_Fixture_Giver";

        private const string ReachStageId = "Reach";

        private const string GatherStageId = "Gather";

        private const string WelcomeNodeId = "Welcome";

        private const string ProposalNodeId = "Proposal";

        private const string UnderwayNodeId = "Underway";

        private const string WelcomeLineKey = "Dlg_Fixture_Welcome";

        private const string AskErrandOptionId = "AskErrand";

        private const string GoodbyeOptionId = "Goodbye";

        /// <summary>A two-stage quest writing a condition or an action in each place a linear quest holds one.</summary>
        private const string QuestsJson = $$"""
        {
          "quests": [
            {
              "id": "{{QuestId}}",
              "giverNpcId": "{{GiverNpcId}}",
              "faction": "Human",
              "turnInNpcIds": [ "{{GiverNpcId}}" ],
              "declinePolicy": "Cooldown",
              "acceptConditions": [ { "type": "Not", "condition": { "type": "Fact", "key": "Fixture_Giver_Offended" } } ],
              "onAccept": [ { "type": "SetFact", "key": "Fixture_Errand_Taken" } ],
              "stages": [
                {
                  "id": "{{ReachStageId}}",
                  "objectives": [ { "id": "Arrive", "condition": { "type": "Fact", "key": "Fixture_Place_Reached" } } ],
                  "onEnter": [ { "type": "SetFact", "key": "Fixture_Errand_Started" } ]
                },
                {
                  "id": "{{GatherStageId}}",
                  "objectives": [
                    { "id": "Evidence", "condition": { "type": "HasItem", "itemId": "Fixture_Evidence", "amount": 2 } },
                    { "id": "Foes", "counter": { "key": "Fixture_Foes_Slain", "amount": 2 }, "optional": true }
                  ],
                  "onComplete": [ { "type": "SetFact", "key": "Fixture_Errand_Done" } ]
                }
              ],
              "rewards": {
                "items": [ { "itemId": "Fixture_Reward", "amount": 1 } ],
                "actions": [ { "type": "TakeItem", "itemId": "Fixture_Evidence", "amount": 2 } ]
              },
              "onDecline": [ { "type": "SetFact", "key": "Fixture_Errand_Declined" } ],
              "onFail": [ { "type": "SetFact", "key": "Fixture_Errand_Failed" } ]
            }
          ]
        }
        """;

        /// <summary>The giver's conversation: an opening for the errand once taken, and a fallback welcome that
        /// offers it, keeps a rumour behind a fact and lets the player leave.</summary>
        private const string DialoguesJson = $$"""
        {
          "dialogues": [
            {
              "npcId": "{{GiverNpcId}}",
              "entryRules": [
                {
                  "priority": 20,
                  "conditions": [ { "type": "QuestStatus", "questId": "{{QuestId}}", "status": "Active" } ],
                  "node": "{{UnderwayNodeId}}"
                },
                { "priority": 0, "conditions": [], "node": "{{WelcomeNodeId}}" }
              ],
              "nodes": [
                {
                  "id": "{{WelcomeNodeId}}",
                  "lines": [ { "speaker": "Npc", "key": "{{WelcomeLineKey}}" } ],
                  "options": [
                    {
                      "id": "{{AskErrandOptionId}}",
                      "key": "Dlg_Fixture_AskErrand",
                      "visibleConditions": [
                        { "type": "CanAcceptQuest", "questId": "{{QuestId}}" },
                        { "type": "QuestOfferRoll", "questId": "{{QuestId}}" }
                      ],
                      "next": "{{ProposalNodeId}}"
                    },
                    {
                      "id": "Rumour",
                      "key": "Dlg_Fixture_Rumour",
                      "visibleConditions": [ { "type": "Fact", "key": "Fixture_Rumour_Heard" } ]
                    },
                    { "id": "{{GoodbyeOptionId}}", "key": "Dlg_Fixture_Goodbye" }
                  ]
                },
                {
                  "id": "{{ProposalNodeId}}",
                  "onEnter": [ { "type": "SetFact", "key": "Fixture_Proposal_Heard" } ],
                  "lines": [ { "speaker": "Npc", "key": "Dlg_Fixture_Proposal" } ],
                  "options": [
                    {
                      "id": "Agree",
                      "key": "Dlg_Fixture_Agree",
                      "enabledConditions": [ { "type": "CanAcceptQuest", "questId": "{{QuestId}}" } ],
                      "actions": [ { "type": "AcceptQuest", "questId": "{{QuestId}}" } ],
                      "next": "{{UnderwayNodeId}}"
                    },
                    {
                      "id": "Decline",
                      "key": "Dlg_Fixture_Decline",
                      "actions": [ { "type": "DeclineQuest", "questId": "{{QuestId}}" } ]
                    }
                  ]
                },
                {
                  "id": "{{UnderwayNodeId}}",
                  "lines": [ { "speaker": "Npc", "key": "Dlg_Fixture_Underway" } ],
                  "options": [ { "id": "{{GoodbyeOptionId}}", "key": "Dlg_Fixture_Goodbye" } ]
                }
              ]
            }
          ]
        }
        """;

        private static readonly string[] s_stageIds = [ReachStageId, GatherStageId];

        private static readonly string[] s_nodeIds = [WelcomeNodeId, ProposalNodeId, UnderwayNodeId];

        private WorldFactsService _facts = null!;
        private GameEventBus _events = null!;
        private IInfluenceMastery _influence = null!;
        private IRandomNumberGenerator _rnd = null!;
        private QuestProvider _quests = null!;
        private DialogueProvider _dialogues = null!;
        private QuestLogService _questLog = null!;

        [TestInitialize]
        public void Setup()
        {
            _facts = new WorldFactsService();
            _events = new GameEventBus();
            _influence = Mock.Of<IInfluenceMastery>();
            _rnd = new Mock<IRandomNumberGenerator>(MockBehavior.Strict).Object;

            var inventory = Mock.Of<IInventory>();
            var clock = Mock.Of<IWorldClock>();
            var relations = Mock.Of<IFactionRelationService>();
            var player = Mock.Of<IPlayerAccessor>();
            var minter = Mock.Of<IItemMinter>();
            var messages = Mock.Of<IGameMessageBus>();

            var conditions = new NarrativeConditionParser(NarrativeFactories.Conditions(
                inventory, _facts, relations, Mock.Of<IPersonalReputationService>(), player, _influence, clock, _rnd,
                () => _questLog, () => _quests));

            var actions = new NarrativeActionParser(NarrativeFactories.Actions(
                _facts, inventory, minter, _events, player, relations, _influence, Mock.Of<IMartialArtMastery>(), messages,
                Mock.Of<INpcProvider>(), Mock.Of<INpcModifierProvider>(), Mock.Of<INpcWorldSpawner>(),
                Mock.Of<INpcPopulationService>(), Mock.Of<ISpawnPointRegistry>(), () => _questLog));

            _quests = new QuestProvider(conditions, actions);
            _dialogues = new DialogueProvider(conditions, actions);
            _quests.Apply(DataCatalog.Quests, new GameDataFile(FixtureFile, QuestsJson));
            _dialogues.Apply(DataCatalog.Dialogues, new GameDataFile(FixtureFile, DialoguesJson));

            _questLog = new QuestLogService(_quests, _facts, inventory, minter, Mock.Of<IUniqueItemQuery>(), _influence,
                clock, Mock.Of<INpcWorldRegistry>(), _events, messages, Mock.Of<ILoadScope>());
        }

        [TestMethod]
        public void QuestsCatalog_ReadsAQuestWholeWithEveryStage()
        {
            var quest = _quests.Get(QuestId);

            Assert.IsNotNull(quest, $"'{QuestId}' was dropped at parse — check the test log for the Tracker report");
            CollectionAssert.AreEqual(s_stageIds, quest.Stages.Select(stage => stage.Id).ToArray(),
                "the quest's stages are not the ones its record writes, in the order written");
        }

        [TestMethod]
        public void DialoguesCatalog_ReadsADialogueWholeWithEveryNode()
        {
            var dialogue = _dialogues.Get(GiverNpcId);

            Assert.IsNotNull(dialogue, $"the dialogue of '{GiverNpcId}' was dropped at parse — check the test log for the Tracker report");
            CollectionAssert.AreEquivalent(s_nodeIds, dialogue.Nodes.Keys.ToArray(), "the dialogue's nodes are not the ones its record writes");
        }

        /// <summary>A file writing its list as null holds no record: the loader says so and reads on, and the
        /// records of every other file stay loaded.</summary>
        [TestMethod]
        public void ACatalogFileWritingItsListAsNull_IsReportedAndNotThrownOver()
        {
            _dialogues.Apply(DataCatalog.Dialogues, new GameDataFile(EmptiedFile, NullDialoguesJson));
            _quests.Apply(DataCatalog.Quests, new GameDataFile(EmptiedFile, NullQuestsJson));

            Assert.IsNotNull(_dialogues.Get(GiverNpcId), "a file holding no dialogue took the read ones with it");
            Assert.IsNotNull(_quests.Get(QuestId), "a file holding no quest took the read ones with it");
        }

        /// <summary>On a fresh world the opening waiting for the errand to be taken does not hold, so the welcome
        /// opens: it offers the errand the quest log can hand out, hides the rumour and keeps the way out.</summary>
        [TestMethod]
        public void DialogueService_OnAFreshWorld_OpensTheFallbackNodeWithOnlyTheOptionsWhoseConditionsHold()
        {
            // An offer roll that passed and has not run out: the condition reads it instead of rolling.
            _facts.SetCount(FactKeys.QuestOfferRollUntil(QuestId), int.MaxValue);
            _facts.SetFact(FactKeys.QuestOfferRollPassed(QuestId));

            var service = new DialogueService(_dialogues, _facts, _influence, _rnd, _events);

            Assert.IsTrue(service.Start(GiverNpcId, npcInstanceId: null, Fractions.Human),
                "no entry rule matched a fresh world — the priority-0 fallback is broken");

            var view = service.Current;
            Assert.IsNotNull(view, "the conversation started and shows nothing");
            Assert.AreEqual(WelcomeLineKey, view.Lines.Single().TextKey,
                "a fresh world opened on the rule waiting for the errand to be taken instead of the fallback welcome");
            CollectionAssert.AreEqual(new[] { AskErrandOptionId, GoodbyeOptionId }, view.Options.Select(option => option.Id).ToArray(),
                "the welcome must show the offer the quest log can hand out and the way out, and hide the option gated on an unset fact");
        }
    }
}
