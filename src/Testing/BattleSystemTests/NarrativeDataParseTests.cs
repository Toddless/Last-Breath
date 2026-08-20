namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Enums;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Dialogues;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Services;
    using Moq;

    /// <summary>
    /// Feeds the REAL SharedData json through the real parsers — a dropped quest/dialogue is a
    /// data bug this catches without booting Godot. Godot's RandomNumberGenerator cannot exist
    /// outside the engine, so it is passed as null and the offer roll is pre-cached in the facts.
    /// </summary>
    [TestClass]
    public class NarrativeDataParseTests
    {
        private const string VeteranNpcId = "Npc_Bandit_Veteran";
        private const string QuestId = "Quest_Field_Of_Bones";

        private WorldFactsService _facts = null!;
        private InfluenceMastery _influence = null!;
        private QuestProvider _quests = null!;
        private DialogueProvider _dialogues = null!;
        private QuestLogService _questLog = null!;
        private GameEventBus _events = null!;

        [TestInitialize]
        public void Setup()
        {
            _facts = new WorldFactsService();
            _events = new GameEventBus();
            _influence = new InfluenceMastery(Mock.Of<Core.MessageBus.IGameMessageBus>());

            var clock = new Mock<Core.Ai.World.Time.IWorldClock>();

            var conditionFactories = new List<INarrativeConditionFactory>
            {
                new HasItemConditionFactory(Mock.Of<Core.Inventory.IInventory>()),
                new FactConditionFactory(_facts),
                new FactionStandingConditionFactory(new Core.Reputation.FactionRelationService(FactionTestData.Create())),
                new NpcRelationConditionFactory(Mock.Of<Core.Reputation.IPersonalReputationService>()),
                new AttributeConditionFactory(Mock.Of<IPlayerAccessor>()),
                new InfluenceConditionFactory(_influence),
                new AllOfConditionFactory(),
                new AnyOfConditionFactory(),
                new NotConditionFactory(),
                new QuestStatusConditionFactory(() => _questLog),
                new CanAcceptQuestConditionFactory(() => _questLog),
                new CanTurnInQuestConditionFactory(() => _questLog),
                new QuestOfferRollConditionFactory(_facts, _influence, clock.Object, null!, () => _quests),
            };

            var inventory = new Mock<Core.Inventory.IInventory>();
            inventory.Setup(mock => mock.GetAvailableCapacity()).Returns(100);

            var actionFactories = new List<INarrativeActionFactory>
            {
                new SetFactActionFactory(_facts),
                new GiveItemActionFactory(Mock.Of<Core.Items.IItemMinter>(), inventory.Object),
                new TakeItemActionFactory(inventory.Object),
                new PublishDeedActionFactory(_events, Mock.Of<IPlayerAccessor>()),
                new AddReputationActionFactory(new Core.Reputation.FactionRelationService(FactionTestData.Create())),
                new AddInfluenceExpActionFactory(_influence),
            };
            foreach (var kind in Enum.GetValues<QuestActionKind>())
                actionFactories.Add(new QuestActionFactory(() => _questLog, kind));

            var conditions = new NarrativeConditionParser(conditionFactories);
            var actions = new NarrativeActionParser(actionFactories);

            _quests = new QuestProvider(conditions, actions);
            _dialogues = new DialogueProvider(conditions, actions);
            ApplyCatalog(_quests, DataCatalog.Quests);
            ApplyCatalog(_dialogues, DataCatalog.Dialogues);

            _questLog = new QuestLogService(_quests, _facts, inventory.Object, Mock.Of<Core.Items.IItemMinter>(),
                Mock.Of<Core.Items.IUniqueItemQuery>(), _influence, clock.Object,
                Mock.Of<Core.Ai.World.Skirmish.INpcWorldRegistry>(),
                _events, Mock.Of<Core.MessageBus.IGameMessageBus>(), Mock.Of<Core.Save.ILoadScope>());
        }

        [TestMethod]
        public void QuestsCatalog_ParsesTheExampleQuest()
        {
            Assert.IsNotNull(_quests.Get(QuestId), $"'{QuestId}' was dropped at parse — check the test log for the Tracker report");
            Assert.AreEqual(2, _quests.Get(QuestId)!.Stages.Count);
        }

        [TestMethod]
        public void DialoguesCatalog_ParsesTheVeteranDialogue()
        {
            var dialogue = _dialogues.Get(VeteranNpcId);
            Assert.IsNotNull(dialogue, $"dialogue for '{VeteranNpcId}' was dropped at parse — check the test log for the Tracker report");
            Assert.AreEqual(8, dialogue!.Nodes.Count);
        }

        [TestMethod]
        public void DialogueService_StartsTheVeteranConversation()
        {
            // Pre-cache the offer roll so IsMet never touches the (absent) Godot RNG.
            _facts.SetCount(FactKeys.QuestOfferRollUntil(QuestId), int.MaxValue);
            _facts.SetCount(FactKeys.QuestOfferRollPassed(QuestId), 1);

            var service = new DialogueService(_dialogues, _facts, _influence, null!, _events);
            bool started = service.Start(VeteranNpcId, npcInstanceId: null, Fractions.Human);

            Assert.IsTrue(started, "no entry rule matched a fresh state — the priority-0 fallback is broken");
            Assert.IsNotNull(service.Current);
            Assert.AreEqual(3, service.Current!.Options.Count, "Greeting must show AskWork + Flatter + Leave on a fresh state");
        }

        [TestMethod]
        public void QuestLog_AcceptsAndTracksTheExampleQuest()
        {
            Assert.IsTrue(_questLog.Accept(QuestId, NarrativeContext.Empty));
            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId));

            _facts.SetFact(FactKeys.LocationDiscovered("Old_Battlefield"));
            Assert.AreEqual(1, _questLog.GetState(QuestId)!.StageIndex, "discovering the battlefield must advance to stage 2");
        }

        private static void ApplyCatalog(IGameDataParticipant participant, string catalog)
        {
            string root = FindSharedData();
            foreach (string file in Directory.GetFiles(Path.Combine(root, catalog), "*.json"))
                participant.Apply(catalog, new GameDataFile(Path.GetFileName(file), File.ReadAllText(file)));
        }

        private static string FindSharedData()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "SharedData");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException("SharedData directory not found above the test binaries");
        }
    }
}
