namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Items;
    using Core.Services;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Save.Participants;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A quest walks the route its stage declares: the first transition whose conditions hold wins,
    /// an unconditional one at the end catches the rest, and a stage whose routes are all still shut
    /// waits on its own stage instead of ending the quest. A named outcome ends the quest and pays
    /// its own rewards instead of the quest-wide ones — or buries it. Linear quests, which declare
    /// none of that, keep walking their list, and a save that named the stage by position still
    /// finds it. The catalog refuses the shapes that cannot be walked at all.
    /// </summary>
    [TestClass]
    public class QuestBranchingTests
    {
        private const string QuestId = "Quest_Branching";
        private const string ChoiceStageId = "Choose";
        private const string LoyalStageId = "SideA";
        private const string BetrayalStageId = "SideB";
        private const string LoyalOutcomeId = "Rewarded";
        private const string BetrayalOutcomeId = "Buried";
        private const string ChoiceFact = "Chose_A";
        private const string PokeFact = "World_Moved";
        private const string CounterFact = "Kill_Count:Npc_Wolf";
        private const string CounterObjectiveId = "Kills";
        private const string FirstStageId = "First";
        private const string SecondStageId = "Second";
        private const string QuestRewardItemId = "Coal";
        private const string OutcomeRewardItemId = "Ornament_Tier_1";
        private const int QuestInfluence = 40;
        private const int OutcomeInfluence = 7;
        private const int LegacySaveVersion = 2;
        private const int CurrentSaveVersion = 3;

        private QuestCatalog _quests = null!;
        private WorldFactsService _facts = null!;
        private GameEventBus _events = null!;
        private Mock<IInfluenceMastery> _influence = null!;
        private List<string> _minted = null!;
        private CountingAction _questReward = null!;
        private CountingAction _outcomeReward = null!;
        private CountingAction _onFail = null!;
        private CountingAction _choiceCompleted = null!;
        private CountingAction _sideCompleted = null!;
        private QuestLogService _questLog = null!;

        [TestInitialize]
        public void Setup()
        {
            _quests = new QuestCatalog();
            _facts = new WorldFactsService();
            _events = new GameEventBus();
            _minted = [];
            _questReward = new CountingAction();
            _outcomeReward = new CountingAction();
            _onFail = new CountingAction();
            _choiceCompleted = new CountingAction();
            _sideCompleted = new CountingAction();
            _influence = new Mock<IInfluenceMastery>();
            _questLog = CreateQuestLog();
        }

        [TestMethod]
        public void ABranch_TakesTheFirstRouteWhoseConditionsHold()
        {
            _facts.SetFact(ChoiceFact);

            AcceptBranchingQuest();

            Assert.AreEqual(LoyalStageId, _questLog.GetState(QuestId)!.StageId,
                "the met condition stands before the unconditional route — the quest took the wrong branch");
            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId));
        }

        [TestMethod]
        public void ABranch_FallsThroughToTheUnconditionalRoute()
        {
            AcceptBranchingQuest();

            Assert.AreEqual(BetrayalStageId, _questLog.GetState(QuestId)!.StageId,
                "no condition held, so the unconditional route at the end had to catch the quest");
        }

        /// <summary>Objectives met and every route shut: the stage stays current and the quest stays
        /// Active, so the next change of the world asks again. Its onComplete belongs to leaving the
        /// stage, so it must not fire once per ask.</summary>
        [TestMethod]
        public void AStageWithNoOpenRoute_WaitsOnItselfWithoutRepeatingItsOnComplete()
        {
            _quests.Add(BranchingQuest(unconditionalFallback: false));
            Accept();

            Poke();
            Poke();

            Assert.AreEqual(ChoiceStageId, _questLog.GetState(QuestId)!.StageId, "a shut branch must not move the quest on");
            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId), "a waiting stage is not a finished quest");
            Assert.AreEqual(0, _choiceCompleted.Executions, "onComplete ran while the quest was still standing on the stage");

            _facts.SetFact(ChoiceFact);

            Assert.AreEqual(LoyalStageId, _questLog.GetState(QuestId)!.StageId, "the route opened and the quest stayed put");
            Assert.AreEqual(1, _choiceCompleted.Executions, "leaving the stage runs its onComplete exactly once");
        }

        [TestMethod]
        public void AnOutcome_PaysItsOwnRewardsInsteadOfTheQuestWideOnes()
        {
            _facts.SetFact(ChoiceFact);
            AcceptBranchingQuest();

            MeetSideObjective();

            Assert.AreEqual(QuestStatus.ReadyToTurnIn, _questLog.GetStatus(QuestId), "an ending waits for the turn-in like any finished quest");
            Assert.AreEqual(LoyalOutcomeId, _questLog.GetState(QuestId)!.OutcomeId, "the quest forgot which ending it reached");

            Assert.IsTrue(_questLog.TurnIn(QuestId, NarrativeContext.Empty));

            CollectionAssert.AreEqual(new[] { OutcomeRewardItemId }, _minted,
                "the turn-in paid the quest-wide reward list; an ending replaces it whole");
            Assert.AreEqual(1, _outcomeReward.Executions);
            Assert.AreEqual(0, _questReward.Executions, "quest-wide reward actions must not run beside an ending's own");
            _influence.Verify(mastery => mastery.AddExperience(OutcomeInfluence), Times.Once);
            _influence.Verify(mastery => mastery.AddExperience(QuestInfluence), Times.Never);
        }

        [TestMethod]
        public void AFailingOutcome_BuriesTheQuestThroughTheOrdinaryFailurePath()
        {
            AcceptBranchingQuest();

            MeetSideObjective();

            Assert.AreEqual(QuestStatus.Failed, _questLog.GetStatus(QuestId), "an outcome declaring itself a failure must bury the quest");
            Assert.AreEqual(BetrayalOutcomeId, _questLog.GetState(QuestId)!.OutcomeId, "a buried quest still remembers the ending it reached");
            Assert.AreEqual(1, _onFail.Executions, "a failing ending is a failure: the quest's onFail runs");
            Assert.AreEqual(0, _minted.Count, "a failed quest pays nothing");
        }

        /// <summary>A quest that declares it cannot fail refuses the burial, but the ending is still an
        /// ending: it stays where it stopped instead of walking out of the stage again on every change
        /// of the world.</summary>
        [TestMethod]
        public void AFailingOutcome_OnAnUnloseableQuest_StopsTheQuestWithoutRepeatingItself()
        {
            _quests.Add(BranchingQuest(unconditionalFallback: true, canFail: false));
            Accept();

            MeetSideObjective();
            Poke();
            Poke();

            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId), "an unloseable quest must not be buried by an ending");
            Assert.AreEqual(BetrayalOutcomeId, _questLog.GetState(QuestId)!.OutcomeId, "the ending was reached and has to be remembered");
            Assert.AreEqual(0, _onFail.Executions, "a refused failure runs nothing");
            Assert.AreEqual(1, _sideCompleted.Executions, "the stage that reached the ending walked out of itself again on the next change of the world");
        }

        /// <summary>The shape every shipped quest has today: no transitions, no outcome. It must keep
        /// walking its list and paying its quest-wide rewards.</summary>
        [TestMethod]
        public void ALinearQuest_StillWalksItsStagesInOrderAndPaysItsOwnRewards()
        {
            var first = new ToggleCondition();
            _quests.Add(LinearQuest(first));
            Accept();

            Assert.AreEqual(FirstStageId, _questLog.GetState(QuestId)!.StageId);

            first.Met = true;
            Poke();

            Assert.AreEqual(SecondStageId, _questLog.GetState(QuestId)!.StageId, "a stage without transitions leads to the next of the list");
            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId));

            _facts.SetCount(CounterFact, 1);

            Assert.AreEqual(QuestStatus.ReadyToTurnIn, _questLog.GetStatus(QuestId), "the last stage of the list ends in a turn-in");
            Assert.IsTrue(_questLog.TurnIn(QuestId, NarrativeContext.Empty));

            CollectionAssert.AreEqual(new[] { QuestRewardItemId }, _minted);
            Assert.AreEqual(1, _questReward.Executions);
            _influence.Verify(mastery => mastery.AddExperience(QuestInfluence), Times.Once);
        }

        [TestMethod]
        public void ASaveNamingTheStageByPosition_FindsItById()
        {
            _quests.Add(LinearQuest(new ToggleCondition()));
            _facts.SetCount(CounterFact, 3);

            new QuestLogSaveParticipant(_questLog, _quests).Restore(JToken.Parse(LegacySaveJson), LegacySaveVersion);

            var state = _questLog.GetState(QuestId);
            Assert.IsNotNull(state, "an old save must still restore its quests");
            Assert.AreEqual(SecondStageId, state!.StageId, "stage #1 of the list is the stage the old file was standing on");
            Assert.AreEqual((0, 1), _questLog.GetObjectiveProgress(QuestId, CounterObjectiveId),
                "the counter snapshot of that stage was keyed by position and had to be re-keyed to the stage id");
        }

        [TestMethod]
        public void ASaveNamingAStageTheQuestLost_RestartsOnTheFirstStage()
        {
            _quests.Add(LinearQuest(new ToggleCondition()));

            new QuestLogSaveParticipant(_questLog, _quests).Restore(JToken.Parse(StrangeStageSaveJson), CurrentSaveVersion);

            Assert.AreEqual(FirstStageId, _questLog.GetState(QuestId)!.StageId,
                "a stage the catalog no longer has must land the quest on the first stage, not on nothing");
        }

        [TestMethod]
        public void TheStageAndTheOutcome_SurviveCaptureAndRestore()
        {
            _facts.SetFact(ChoiceFact);
            AcceptBranchingQuest();
            MeetSideObjective();

            JToken saved = new QuestLogSaveParticipant(_questLog, _quests).Capture();
            var reloaded = CreateQuestLog();
            new QuestLogSaveParticipant(reloaded, _quests).Restore(saved, CurrentSaveVersion);

            var state = reloaded.GetState(QuestId)!;
            Assert.AreEqual(LoyalStageId, state.StageId);
            Assert.AreEqual(LoyalOutcomeId, state.OutcomeId, "the loaded game forgot the ending and would pay the quest-wide rewards");

            Assert.IsTrue(reloaded.TurnIn(QuestId, NarrativeContext.Empty));
            CollectionAssert.AreEqual(new[] { OutcomeRewardItemId }, _minted);
        }

        [TestMethod]
        public void TheCatalog_ParsesABranchingQuest()
        {
            var quest = Parse(BranchingCatalogJson).Get("Quest_Branch");

            Assert.IsNotNull(quest, "the fixture quest was dropped at parse — check the test log for the Tracker report");
            Assert.AreEqual(2, quest!.Stages[0].Transitions.Count);
            Assert.AreEqual(1, quest.Stages[0].Transitions[0].Conditions.Count, "the transition's condition was not parsed");
            Assert.AreEqual(0, quest.Stages[1].Transitions.Count, "an empty transition list reads as the next stage of the list");
            Assert.AreEqual("Betrayed", quest.Stages[2].Outcome!.Id);
            Assert.IsTrue(quest.Stages[2].Outcome!.Fails);
        }

        /// <summary>A stage no route reaches is unfinished authoring, not a quest that cannot be walked.</summary>
        [TestMethod]
        public void TheCatalog_KeepsAQuestWithAnUnreachableStage()
        {
            Assert.IsNotNull(Parse(UnreachableStageJson).Get("Quest_Orphan"));
        }

        [TestMethod]
        [DataRow(DanglingTargetJson, DisplayName = "a route to a stage the quest does not have")]
        [DataRow(CycleJson, DisplayName = "a stage reachable from itself")]
        [DataRow(SelfRouteJson, DisplayName = "a route back to its own stage")]
        [DataRow(OutcomeWithTransitionsJson, DisplayName = "an ending that also declares routes")]
        [DataRow(DuplicateStageIdJson, DisplayName = "two stages sharing an id")]
        [DataRow(DuplicateOutcomeIdJson, DisplayName = "two stages ending on the same outcome")]
        public void TheCatalog_DropsAQuestItCannotWalk(string json)
        {
            Assert.IsNull(Parse(json).Get("Quest_Broken"), "a quest whose stages cannot be walked must be dropped whole");
        }

        private QuestProvider Parse(string json)
        {
            var provider = new QuestProvider(new NarrativeConditionParser([new FactConditionFactory(_facts)]), new NarrativeActionParser([]));
            provider.Apply(DataCatalog.Quests, new GameDataFile("test.json", json));
            return provider;
        }

        private QuestLogService CreateQuestLog()
        {
            var inventory = new Mock<Core.Inventory.IInventory>();
            inventory.Setup(bag => bag.GetAvailableCapacity()).Returns(100);
            inventory.Setup(bag => bag.TryAddItem(It.IsAny<IItem>(), It.IsAny<int>()))
                .Callback((IItem item, int _) => _minted.Add(item.Id))
                .Returns(true);

            var minter = new Mock<IItemMinter>();
            minter.Setup(mint => mint.MintItem(It.IsAny<string>(), It.IsAny<Rarity?>()))
                .Returns((string id, Rarity? _) => Mock.Of<IItem>(item => item.Id == id));

            return new QuestLogService(_quests, _facts, inventory.Object, minter.Object, Mock.Of<IUniqueItemQuery>(),
                _influence.Object, Mock.Of<IWorldClock>(), Mock.Of<INpcWorldRegistry>(), _events,
                Mock.Of<Core.MessageBus.IGameMessageBus>(), Mock.Of<Core.Save.ILoadScope>());
        }

        private void AcceptBranchingQuest()
        {
            _quests.Add(BranchingQuest(unconditionalFallback: true));
            Accept();
        }

        private void Accept() =>
            Assert.IsTrue(_questLog.Accept(QuestId, NarrativeContext.Empty), "the fixture quest must be acceptable");

        /// <summary>Both branches wait on the same fact, so one call finishes whichever the quest took.</summary>
        private void MeetSideObjective() => _facts.SetCount(CounterFact, 1);

        /// <summary>Moves the world without touching anything a quest objective reads.</summary>
        private void Poke() => _facts.Add(PokeFact);

        /// <summary>One choice and two endings: the loyal branch pays its own rewards, the other buries
        /// the quest. Without the unconditional fallback the choice stage has nowhere to go until the
        /// fact is set.</summary>
        private QuestDefinition BranchingQuest(bool unconditionalFallback, bool canFail = true)
        {
            List<QuestStageTransition> routes = [new(LoyalStageId, [new FactCondition(_facts, ChoiceFact, 1)])];
            if (unconditionalFallback) routes.Add(new QuestStageTransition(BetrayalStageId, []));

            return Quest(canFail,
                new QuestStageDefinition(ChoiceStageId, [Objective(new MetCondition())], [], [_choiceCompleted], routes, null),
                new QuestStageDefinition(LoyalStageId, [Counter()], [], [_sideCompleted], [],
                    new QuestOutcomeDefinition(LoyalOutcomeId, false,
                        new QuestRewards(OutcomeInfluence, [new QuestRewardItem(OutcomeRewardItemId, 1)], [_outcomeReward]))),
                new QuestStageDefinition(BetrayalStageId, [Counter()], [], [_sideCompleted], [],
                    new QuestOutcomeDefinition(BetrayalOutcomeId, true, new QuestRewards(0, [], []))));
        }

        private QuestDefinition LinearQuest(INarrativeCondition first) =>
            Quest(canFail: true,
                new QuestStageDefinition(FirstStageId, [Objective(first)], [], [], [], null),
                new QuestStageDefinition(SecondStageId, [Counter()], [], [], [], null));

        private QuestDefinition Quest(bool canFail, params QuestStageDefinition[] stages) =>
            new(QuestId, string.Empty, Fractions.Human, 1, false, [], DeclinePolicy.CanReturn, 0, canFail, 0,
                [], stages,
                new QuestRewards(QuestInfluence, [new QuestRewardItem(QuestRewardItemId, 1)], [_questReward]),
                [], [], [_onFail]);

        private static QuestObjectiveDefinition Objective(INarrativeCondition condition) =>
            new("Objective", condition, null, false, false);

        /// <summary>A non-retroactive counter: what it saw on entering the stage is the baseline a save
        /// has to carry over.</summary>
        private static QuestObjectiveDefinition Counter() =>
            new(CounterObjectiveId, null, new QuestCounter(CounterFact, 1, Retroactive: false), false, false);

        private const string LegacySaveJson = $$"""
        {
          "quests": [
            {
              "questId": "{{QuestId}}",
              "status": "Active",
              "stageIndex": 1,
              "counterBaselines": { "1:{{CounterFact}}": 3 },
              "ghostHintShown": false,
              "acceptedAtMinutes": 0,
              "nextOfferAtMinutes": 0
            }
          ]
        }
        """;

        private const string StrangeStageSaveJson = $$"""
        {
          "quests": [
            {
              "questId": "{{QuestId}}",
              "status": "Active",
              "stageId": "A_Stage_The_Patch_Renamed",
              "counterBaselines": {},
              "ghostHintShown": false,
              "acceptedAtMinutes": 0,
              "nextOfferAtMinutes": 0
            }
          ]
        }
        """;

        private const string BranchingCatalogJson = """
        {
          "quests": [
            {
              "id": "Quest_Branch",
              "stages": [
                {
                  "id": "Choose",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [
                    { "to": "Loyal", "conditions": [ { "type": "Fact", "key": "Chose_A" } ] },
                    { "to": "Betray" }
                  ]
                },
                {
                  "id": "Loyal",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": []
                },
                {
                  "id": "Betray",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "outcome": { "id": "Betrayed", "fails": true }
                }
              ]
            }
          ]
        }
        """;

        private const string UnreachableStageJson = """
        {
          "quests": [
            {
              "id": "Quest_Orphan",
              "stages": [
                {
                  "id": "A",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [ { "to": "C" } ]
                },
                { "id": "B", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] },
                { "id": "C", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] }
              ]
            }
          ]
        }
        """;

        private const string DanglingTargetJson = """
        {
          "quests": [
            {
              "id": "Quest_Broken",
              "stages": [
                {
                  "id": "A",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [ { "to": "Nowhere" } ]
                }
              ]
            }
          ]
        }
        """;

        private const string CycleJson = """
        {
          "quests": [
            {
              "id": "Quest_Broken",
              "stages": [
                {
                  "id": "A",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [ { "to": "B" } ]
                },
                {
                  "id": "B",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [ { "to": "A" } ]
                }
              ]
            }
          ]
        }
        """;

        private const string SelfRouteJson = """
        {
          "quests": [
            {
              "id": "Quest_Broken",
              "stages": [
                {
                  "id": "A",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [ { "to": "A" } ]
                }
              ]
            }
          ]
        }
        """;

        private const string OutcomeWithTransitionsJson = """
        {
          "quests": [
            {
              "id": "Quest_Broken",
              "stages": [
                {
                  "id": "A",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [ { "to": "B" } ],
                  "outcome": { "id": "Done" }
                },
                { "id": "B", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] }
              ]
            }
          ]
        }
        """;

        private const string DuplicateStageIdJson = """
        {
          "quests": [
            {
              "id": "Quest_Broken",
              "stages": [
                { "id": "A", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] },
                { "id": "A", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] }
              ]
            }
          ]
        }
        """;

        private const string DuplicateOutcomeIdJson = """
        {
          "quests": [
            {
              "id": "Quest_Broken",
              "stages": [
                {
                  "id": "A",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "transitions": [ { "to": "B" }, { "to": "C" } ]
                },
                {
                  "id": "B",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "outcome": { "id": "Done" }
                },
                {
                  "id": "C",
                  "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ],
                  "outcome": { "id": "Done" }
                }
              ]
            }
          ]
        }
        """;

        private sealed class MetCondition : INarrativeCondition
        {
            public bool IsMet(NarrativeContext context) => true;
        }

        private sealed class ToggleCondition : INarrativeCondition
        {
            public bool Met { get; set; }

            public bool IsMet(NarrativeContext context) => Met;
        }

        private sealed class CountingAction : INarrativeAction
        {
            public int Executions { get; private set; }

            public void Execute(NarrativeContext context) => Executions++;
        }

        private sealed class QuestCatalog : IQuestProvider
        {
            private readonly Dictionary<string, QuestDefinition> _quests = [];

            public IReadOnlyCollection<QuestDefinition> All => _quests.Values;

            public QuestDefinition? Get(string questId) => _quests.GetValueOrDefault(questId);

            public void Add(QuestDefinition quest) => _quests[quest.Id] = quest;
        }
    }
}
