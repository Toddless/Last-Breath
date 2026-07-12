namespace LastBreathTest.BattleSystemTests
{
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;
    using Core.Narrative.Dialogues;
    using Core.Services;
    using Core.Views.UI;
    using Moq;

    [TestClass]
    public class UiContextServiceTests
    {
        private GameEventBus _bus = null!;
        private PlayerAccessor _accessor = null!;
        private UiContextService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _bus = new GameEventBus();
            _accessor = new PlayerAccessor();
            _service = new UiContextService(_bus, _accessor);
        }

        [TestMethod]
        public void BattleSwitchesInAndOut()
        {
            _bus.Publish(new BattleInitializedEvent(Mock.Of<IFightable>(), []));
            Assert.AreEqual(UiContext.Battle, _service.Current);

            _bus.Publish(new BattleEndEvent(BattleResults.PlayerWon));
            Assert.AreEqual(UiContext.World, _service.Current);
        }

        [TestMethod]
        public void LostBattleLeadsToDefeatedThenReviveOrGameOver()
        {
            _bus.Publish(new BattleInitializedEvent(Mock.Of<IFightable>(), []));
            _bus.Publish(new BattleEndEvent(BattleResults.PlayerLost));
            Assert.AreEqual(UiContext.Defeated, _service.Current);

            _bus.Publish(new PlayerRevivedEvent());
            Assert.AreEqual(UiContext.World, _service.Current);

            _bus.Publish(new BattleEndEvent(BattleResults.PlayerLost));
            _bus.Publish(new PlayerFinalDeathEvent());
            Assert.AreEqual(UiContext.GameOver, _service.Current);
        }

        [TestMethod]
        public void FreshPlayerSettlesTheWorld()
        {
            _bus.Publish(new PlayerFinalDeathEvent());
            Assert.AreEqual(UiContext.GameOver, _service.Current);

            // A save load reloads the scene; the fresh player registers and the context recovers.
            _accessor.Set(Mock.Of<IPlayer>());
            Assert.AreEqual(UiContext.World, _service.Current);
        }

        [TestMethod]
        public void DialogueSwitchesOnlyFromWorldAndBattleWinsTheRace()
        {
            bool active = false;
            var dialogues = new Mock<IDialogueService>();
            dialogues.SetupGet(d => d.IsActive).Returns(() => active);
            var service = new UiContextService(_bus, _accessor, dialogues.Object);

            active = true;
            dialogues.Raise(d => d.Changed += null);
            Assert.AreEqual(UiContext.Dialogue, service.Current);

            // The battle rips the conversation: Ended must not downgrade the battle context.
            _bus.Publish(new BattleInitializedEvent(Mock.Of<IFightable>(), []));
            active = false;
            dialogues.Raise(d => d.Ended += null);
            Assert.AreEqual(UiContext.Battle, service.Current);
        }

        [TestMethod]
        public void SessionResetReturnsToWorld()
        {
            _bus.Publish(new PlayerFinalDeathEvent());
            _service.ResetSession();
            Assert.AreEqual(UiContext.World, _service.Current);
        }
    }

    [TestClass]
    public class UiWindowGatingTests
    {
        private GameEventBus _bus = null!;
        private UiContextService _context = null!;
        private UiElementsManager _manager = null!;

        [TestInitialize]
        public void Setup()
        {
            _bus = new GameEventBus();
            _context = new UiContextService(_bus, new PlayerAccessor());
            var provider = new Mock<IGameServiceProvider>();
            provider.Setup(p => p.GetServices<IUiContextService>()).Returns([_context]);
            _manager = new UiElementsManager(provider.Object);
        }

        [TestMethod]
        public void ForbiddenContextIsASilentNoOp()
        {
            _manager.RegisterWindowFactory(typeof(FakeWindow), () => new FakeWindow(), UiContext.World);

            _bus.Publish(new BattleInitializedEvent(Mock.Of<IFightable>(), []));
            Assert.IsNull(_manager.ToggleWindow(typeof(FakeWindow)));

            _bus.Publish(new BattleEndEvent(BattleResults.PlayerWon));
            Assert.IsNotNull(_manager.ToggleWindow(typeof(FakeWindow)));
        }

        [TestMethod]
        public void ContextSwitchClosesDisallowedWindows()
        {
            _manager.RegisterWindowFactory(typeof(FakeWindow), () => new FakeWindow(), UiContext.World);
            _manager.RegisterWindowFactory(typeof(EverywhereWindow), () => new EverywhereWindow());

            var worldOnly = (FakeWindow)_manager.OpenWindow(typeof(FakeWindow))!;
            var everywhere = (EverywhereWindow)_manager.OpenWindow(typeof(EverywhereWindow))!;

            _bus.Publish(new BattleInitializedEvent(Mock.Of<IFightable>(), []));

            Assert.IsTrue(worldOnly.Closed, "a world-only window must slam shut when the battle starts");
            Assert.IsFalse(everywhere.Closed);
        }

        private class FakeWindow : IWindow
        {
            public bool Closed { get; private set; }
            public bool IsAlreadyVisible => false;
            public void Close() => Closed = true;
            public void InjectServices(IGameServiceProvider provider)
            {
            }
        }

        private sealed class EverywhereWindow : FakeWindow;
    }
}
