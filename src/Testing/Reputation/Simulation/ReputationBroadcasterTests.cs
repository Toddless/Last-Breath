namespace LastBreathTest.Reputation.Simulation
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Events;
    using Core.Localization;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Core.Reputation;
    using Core.Save;
    using Core.Services;
    using Moq;

    [TestClass]
    public class ReputationBroadcasterTests
    {
        private GameEventBus _bus = null!;
        private FactionRelationService _relations = null!;
        private LoadScope _loadScope = null!;
        private List<SendNotificationMessageMessage> _toasts = null!;
        private List<ReputationChangedEvent> _repEvents = null!;
        private List<PlayerStandingChangedEvent> _standingEvents = null!;

        [TestInitialize]
        public void Setup()
        {
            var localization = new Mock<ILocalizationService>();
            localization.Setup(l => l.Localize(It.IsAny<string>())).Returns<string>(key => key);
            Localization.Override(localization.Object);

            _bus = new GameEventBus();
            _relations = new FactionRelationService(FactionTestData.Create());
            _loadScope = new LoadScope();
            _toasts = [];
            _repEvents = [];
            _standingEvents = [];

            var messages = new Mock<IGameMessageBus>();
            messages.Setup(m => m.PublishMessageAsync(It.IsAny<SendNotificationMessageMessage>()))
                .Callback<SendNotificationMessageMessage>(_toasts.Add)
                .Returns(Task.CompletedTask);

            _bus.Subscribe<ReputationChangedEvent>(_repEvents.Add);
            _bus.Subscribe<PlayerStandingChangedEvent>(_standingEvents.Add);

            _ = new ReputationBroadcaster(_relations, _bus, messages.Object, _loadScope);
        }

        [TestMethod]
        public void DeedChangeRepublishesAndToasts()
        {
            _relations.AddReputation(Fractions.Elf, -120, "Deed_Kill_Npc");

            Assert.AreEqual(1, _repEvents.Count);
            Assert.AreEqual(-120, _repEvents[0].Change.Delta);
            Assert.AreEqual(1, _toasts.Count);
            Assert.AreEqual("UI_Reputation_Changed", _toasts[0].Id);
            Assert.AreEqual("-120", _toasts[0].Values?["Delta"]);
            Assert.AreEqual("Fraction_Elf", _toasts[0].Values?["Faction"]);
        }

        [TestMethod]
        public void ThresholdCrossingAddsAStandingToast()
        {
            _relations.AddReputation(Fractions.Elf, 1100, "Deed_Test"); // Neutral → Friendly

            Assert.AreEqual(1, _standingEvents.Count);
            Assert.AreEqual(RelationLevel.Friendly, _standingEvents[0].Level);
            Assert.AreEqual(2, _toasts.Count); // points toast + standing toast
            Assert.AreEqual("UI_Standing_Changed", _toasts[1].Id);
            Assert.AreEqual("RelationLevel_Friendly", _toasts[1].Values?["Level"]);
        }

        [TestMethod]
        public void DirectSetsRepublishButNeverToast()
        {
            _relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Respect);

            Assert.AreEqual(1, _repEvents.Count);
            Assert.AreEqual(1, _standingEvents.Count);
            Assert.AreEqual(1, _toasts.Count); // only the standing toast, no points toast
            Assert.AreEqual("UI_Standing_Changed", _toasts[0].Id);
        }

        [TestMethod]
        public void EverythingIsMutedWhileLoading()
        {
            using (_loadScope.Begin())
                _relations.AddReputation(Fractions.Elf, -120, "Deed_Kill_Npc");

            Assert.AreEqual(0, _repEvents.Count);
            Assert.AreEqual(0, _toasts.Count);
        }
    }
}
