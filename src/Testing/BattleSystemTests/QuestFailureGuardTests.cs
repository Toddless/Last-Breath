namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Data.GameData;
    using Core.Data.QuestData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Core.Narrative.Facts;
    using Core.Narrative.Quests;
    using Core.Services;
    using Moq;
    using Newtonsoft.Json;

    /// <summary>
    /// A quest that declares it cannot fail survives every path to Failed: the giver rising as
    /// undead, a deadline running out, a direct Fail call, and the player turning the quest down
    /// or dropping an accepted one under a Fail policy. Each unloseable case is paired with the
    /// same scenario on a normal quest — that pair proves the scenario really reaches the failure
    /// path instead of quietly doing nothing.
    /// </summary>
    [TestClass]
    public class QuestFailureGuardTests
    {
        private const string TrainerQuestId = "Quest_Field_Of_Bones";
        private const string QuestId = "Quest_Test";
        private const string TurnInNpcId = "Npc_Test_Giver";
        private const string TriggerFact = "Test_Trigger";
        private const int MinutesPerHour = 60;

        private WorldFactsService _facts = null!;
        private GameEventBus _events = null!;
        private QuestCatalog _quests = null!;
        private NpcRegistry _registry = null!;
        private QuestLogService _questLog = null!;
        private int _minuteOfDay;

        [TestInitialize]
        public void Setup()
        {
            _facts = new WorldFactsService();
            _events = new GameEventBus();
            _quests = new QuestCatalog();
            _registry = new NpcRegistry();
            _minuteOfDay = 0;

            var clock = new Mock<IWorldClock>();
            clock.Setup(timeline => timeline.MinuteOfDay).Returns(() => _minuteOfDay);

            _questLog = new QuestLogService(_quests, _facts, Mock.Of<Core.Inventory.IInventory>(),
                Mock.Of<Core.Items.IItemMinter>(), Mock.Of<Core.Narrative.Influence.IInfluenceMastery>(),
                clock.Object, _registry, _events, Mock.Of<Core.MessageBus.IGameMessageBus>(),
                Mock.Of<Core.Save.ILoadScope>());
        }

        [TestMethod]
        public void RisenTurnInNpc_LeavesAnUnloseableQuestAlive()
        {
            AcceptQuest(Quest(canFail: false));

            RiseTurnInNpcAsUndead();

            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId),
                "the giver rising as undead emptied the turn-in pool and buried a quest that declares it cannot fail");
        }

        [TestMethod]
        public void RisenTurnInNpc_StillFailsAnOrdinaryQuest()
        {
            AcceptQuest(Quest(canFail: true));

            RiseTurnInNpcAsUndead();

            Assert.AreEqual(QuestStatus.Failed, _questLog.GetStatus(QuestId),
                "an ordinary quest must still fail when its last turn-in candidate is gone");
        }

        [TestMethod]
        public void Fail_TellsTheCallerNothingWasBuried()
        {
            AcceptQuest(Quest(canFail: false));

            Assert.IsFalse(_questLog.Fail(QuestId, "Test"), "a refused failure must answer false, not pretend it happened");
            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId));
        }

        [TestMethod]
        public void Fail_ReportsSuccessOnAnOrdinaryQuest()
        {
            AcceptQuest(Quest(canFail: true));

            Assert.IsTrue(_questLog.Fail(QuestId, "Test"));
            Assert.AreEqual(QuestStatus.Failed, _questLog.GetStatus(QuestId));
        }

        [TestMethod]
        public void Deadline_DoesNotBuryAnUnloseableQuest()
        {
            var objective = new ToggleCondition();
            AcceptQuest(Quest(canFail: false, timeLimitHours: 1, objective: objective));

            PassMinutes(2 * MinutesPerHour);

            Assert.AreEqual(QuestStatus.Active, _questLog.GetStatus(QuestId), "the deadline buried a quest that cannot fail");

            objective.Met = true;
            PassMinutes(1);

            Assert.AreEqual(QuestStatus.ReadyToTurnIn, _questLog.GetStatus(QuestId),
                "an unloseable quest must keep advancing after its deadline, not freeze on it");
        }

        [TestMethod]
        public void Deadline_StillBuriesAnOrdinaryQuest()
        {
            AcceptQuest(Quest(canFail: true, timeLimitHours: 1));

            PassMinutes(2 * MinutesPerHour);

            Assert.AreEqual(QuestStatus.Failed, _questLog.GetStatus(QuestId), "a deadline must still fail an ordinary quest");
        }

        [TestMethod]
        public void Abandon_LeavesAnUnloseableFailPolicyQuestOfferable()
        {
            AcceptQuest(Quest(canFail: false, policy: DeclinePolicy.Fail));

            _questLog.Abandon(QuestId);

            Assert.AreEqual(QuestStatus.Declined, _questLog.GetStatus(QuestId),
                "dropping an unloseable quest must return it to the offer, not bury it or leave it half-active");
        }

        [TestMethod]
        public void Decline_LeavesAnUnloseableFailPolicyQuestOfferable()
        {
            _quests.Add(Quest(canFail: false, policy: DeclinePolicy.Fail));

            _questLog.Decline(QuestId, NarrativeContext.Empty);

            Assert.AreEqual(QuestStatus.Declined, _questLog.GetStatus(QuestId),
                "turning an unloseable quest down must leave it offerable, not stuck in the accepted state");
        }

        [TestMethod]
        public void Abandon_StillBuriesAnOrdinaryFailPolicyQuest()
        {
            AcceptQuest(Quest(canFail: true, policy: DeclinePolicy.Fail));

            _questLog.Abandon(QuestId);

            Assert.AreEqual(QuestStatus.Failed, _questLog.GetStatus(QuestId), "a Fail policy must still bury an ordinary quest");
        }

        [TestMethod]
        public void Decline_StillBuriesAnOrdinaryFailPolicyQuest()
        {
            _quests.Add(Quest(canFail: true, policy: DeclinePolicy.Fail));

            _questLog.Decline(QuestId, NarrativeContext.Empty);

            Assert.AreEqual(QuestStatus.Failed, _questLog.GetStatus(QuestId),
                "a Fail policy must still bury an ordinary quest the player turns down");
        }

        [TestMethod]
        public void QuestCatalog_ReadsTheFlagAndDefaultsToFailable()
        {
            var provider = new QuestProvider(new NarrativeConditionParser([]), new NarrativeActionParser([]));
            provider.Apply(DataCatalog.Quests, new GameDataFile("test.json", CatalogJson));

            var unloseable = provider.Get("Quest_Unloseable");
            var silent = provider.Get("Quest_Silent");

            Assert.IsNotNull(unloseable, "the fixture quest was dropped at parse — check the test log for the Tracker report");
            Assert.IsNotNull(silent);
            Assert.IsFalse(unloseable.CanFail, "\"canFail\": false in the catalog did not reach the definition");
            Assert.IsTrue(silent.CanFail, "a quest that says nothing about failing must keep failing as before");
        }

        [TestMethod]
        public void ShippedTrainerQuest_DeclaresItselfUnloseable()
        {
            string json = File.ReadAllText(Path.Combine(SharedData.Catalog(DataCatalog.Quests), "Quests.json"));
            var quest = JsonConvert.DeserializeObject<QuestsData>(json)!.Quests.Single(entry => entry.Id == TrainerQuestId);

            Assert.IsFalse(quest.CanFail, $"'{TrainerQuestId}' is a teaching chain — it must not be loseable");
        }

        private const string CatalogJson = """
        {
          "quests": [
            {
              "id": "Quest_Unloseable",
              "canFail": false,
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            },
            {
              "id": "Quest_Silent",
              "stages": [ { "id": "S", "objectives": [ { "id": "O", "counter": { "key": "K", "amount": 1 } } ] } ]
            }
          ]
        }
        """;

        private static QuestDefinition Quest(bool canFail, DeclinePolicy policy = DeclinePolicy.CanReturn,
            int timeLimitHours = 0, INarrativeCondition? objective = null) =>
            new(QuestId, TurnInNpcId, Fractions.Human, 1, false, [TurnInNpcId], policy, 0, canFail, timeLimitHours,
                [],
                [new QuestStageDefinition("Stage", [new QuestObjectiveDefinition("Objective", objective ?? new ToggleCondition(), null, false, false)], [], [])],
                new QuestRewards(0, [], []), [], [], []);

        private static ISkirmishParticipant Npc(Fractions faction)
        {
            var npc = new Mock<ISkirmishParticipant>();
            npc.Setup(participant => participant.Fraction).Returns(faction);
            npc.Setup(participant => participant.IsAlive).Returns(true);
            npc.As<IEntity>().Setup(entity => entity.Id).Returns(TurnInNpcId);
            return npc.Object;
        }

        private void AcceptQuest(QuestDefinition quest)
        {
            _quests.Add(quest);
            _registry.All = [Npc(Fractions.Human)];
            Assert.IsTrue(_questLog.Accept(QuestId, NarrativeContext.Empty), "the fixture quest must be acceptable");
        }

        /// <summary>What the body lifecycle does when the resurrection timer runs out: the NPC keeps
        /// living in the registry under another faction, so the quest's turn-in pool is empty.</summary>
        private void RiseTurnInNpcAsUndead()
        {
            _registry.All = [Npc(Fractions.Undead)];
            _events.Publish(new NpcFactionChangedEvent("instance", TurnInNpcId, Fractions.Human, Fractions.Undead, Godot.Vector2.Zero));
        }

        /// <summary>Moves the clock and pokes the world so the quest log re-evaluates.</summary>
        private void PassMinutes(int minutes)
        {
            _minuteOfDay += minutes;
            _facts.Add(TriggerFact);
        }

        private sealed class ToggleCondition : INarrativeCondition
        {
            public bool Met { get; set; }

            public bool IsMet(NarrativeContext context) => Met;
        }

        private sealed class QuestCatalog : IQuestProvider
        {
            private readonly Dictionary<string, QuestDefinition> _quests = [];

            public IReadOnlyCollection<QuestDefinition> All => _quests.Values;

            public QuestDefinition? Get(string questId) => _quests.GetValueOrDefault(questId);

            public void Add(QuestDefinition quest) => _quests[quest.Id] = quest;
        }

        private sealed class NpcRegistry : INpcWorldRegistry
        {
            public IReadOnlyList<ISkirmishParticipant> All { get; set; } = [];

            public void Register(ISkirmishParticipant npc) => All = [.. All, npc];

            public void Unregister(ISkirmishParticipant npc) => All = [.. All.Where(entry => entry != npc)];
        }
    }
}
