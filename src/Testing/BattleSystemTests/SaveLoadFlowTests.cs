namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.Narrative.Facts;
    using Core.Narrative.Influence;
    using Core.Narrative.Quests;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Moq;

    [TestClass]
    public class SaveLoadFlowTests
    {
        private const string QuestPinId = "Quest_Pin";

        [TestMethod]
        public void PopulationResetDropsTheCounterForSceneReload()
        {
            var population = new NpcPopulationService(new Mock<IGameEventBus>().Object) { GlobalLimit = 2 };
            Assert.IsTrue(population.TryReserve());
            Assert.IsTrue(population.TryReserve());
            Assert.IsFalse(population.TryReserve());

            population.Reset();

            Assert.AreEqual(0, population.CurrentCount);
            Assert.IsTrue(population.TryReserve());
        }

        [TestMethod]
        public void RestoringAnEarlierSaveTakesTheLaterLayoutApart()
        {
            // The file was written with the ability on the first slot; the player has since moved it.
            // The restore lays the bar out as the file left it, so the slot it does not name empties.
            var sourceBook = new AbilityBookComponent(new Mock<IFightable>().Object);
            sourceBook.Learn(Stance.Dexterity, FakeAbility("Ability_A"));
            var captured = new AbilityBookSaveParticipant(AccessorFor(sourceBook)).Capture();

            var targetBook = new AbilityBookComponent(new Mock<IFightable>().Object);
            var moved = FakeAbility("Ability_A");
            targetBook.Learn(Stance.Dexterity, moved);
            targetBook.Equip(Stance.Dexterity, moved.InstanceId, 3);
            var participant = new AbilityBookSaveParticipant(AccessorFor(targetBook));

            participant.Restore(captured, participant.Version);

            Assert.AreEqual("Ability_A", targetBook.GetSlotLayout(Stance.Dexterity)[0]?.Id);
            Assert.IsNull(targetBook.GetSlotLayout(Stance.Dexterity)[3]);
        }

        /// <summary>The journal of the playthrough being left behind is replaced without a single status
        /// event, so a view that drew it in its own _Ready is never asked to draw it again. That silence
        /// is deliberate — it is why the applied load announces itself instead.</summary>
        [TestMethod]
        public void RestoringTheQuestLogAnnouncesNothing()
        {
            var events = new GameEventBus();
            var catalog = new SingleQuest(QuestPinId);
            var questLog = new QuestLogService(catalog, new WorldFactsService(), Mock.Of<IInventory>(),
                Mock.Of<IItemMinter>(), Mock.Of<IUniqueItemQuery>(), Mock.Of<IInfluenceMastery>(),
                Mock.Of<IWorldClock>(), Mock.Of<INpcWorldRegistry>(), events, Mock.Of<IGameMessageBus>(),
                Mock.Of<ILoadScope>());
            int announcements = 0;
            events.Subscribe<QuestStatusChangedEvent>(_ => announcements++);
            events.Subscribe<QuestStageAdvancedEvent>(_ => announcements++);

            questLog.RestoreState([new QuestState(QuestPinId) { Status = QuestStatus.Active }]);

            Assert.AreEqual(1, questLog.States.Count, "the restored state has to reach the journal");
            Assert.AreEqual(0, announcements, "the restore is silent by design");
        }

        /// <summary>A catalog holding one quest: the restore drops states whose quest it cannot find.</summary>
        private sealed class SingleQuest(string questId) : IQuestProvider
        {
            private readonly QuestDefinition _quest = new(questId, string.Empty, null, 1, false, [],
                DeclinePolicy.CanReturn, 0, true, 0, [], [], new QuestRewards(0, [], []), [], [], []);

            public IReadOnlyCollection<QuestDefinition> All => [_quest];

            public QuestDefinition? Get(string id) => id == questId ? _quest : null;
        }

        private static IPlayerAccessor AccessorFor(AbilityBookComponent book)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.AbilityBook).Returns(book);
            var accessor = new Mock<IPlayerAccessor>();
            accessor.SetupGet(a => a.Player).Returns(player.Object);
            return accessor.Object;
        }

        private static IAbility FakeAbility(string id)
        {
            string instanceId = Guid.NewGuid().ToString();
            var ability = new Mock<IAbility>();
            ability.SetupGet(a => a.Id).Returns(id);
            ability.SetupGet(a => a.InstanceId).Returns(instanceId);
            ability.Setup(a => a.IsSame(It.IsAny<string>())).Returns((string other) => other == instanceId);
            ability.SetupGet(a => a.InstalledUpgrades).Returns(new Dictionary<string, IAugment>());
            return ability.Object;
        }
    }
}
