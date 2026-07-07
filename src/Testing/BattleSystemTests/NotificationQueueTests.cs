namespace LastBreathTest.BattleSystemTests
{
    using Core.Services;
    using Core.Views.UI;

    [TestClass]
    public class NotificationQueueTests
    {
        private NotificationQueue _queue = null!;

        [TestInitialize]
        public void Setup() => _queue = new NotificationQueue(maxVisiblePerRegion: 2);

        [TestMethod]
        public void TakesSlotsUpToTheLimitThenParks()
        {
            Assert.IsTrue(_queue.TryTake("a", OverlayRegion.BottomRight));
            Assert.IsTrue(_queue.TryTake("b", OverlayRegion.BottomRight));
            Assert.IsFalse(_queue.TryTake("c", OverlayRegion.BottomRight));
        }

        [TestMethod]
        public void ReleaseHandsOutQueuedIdsInFifoOrder()
        {
            _queue.TryTake("a", OverlayRegion.BottomRight);
            _queue.TryTake("b", OverlayRegion.BottomRight);
            _queue.TryTake("c", OverlayRegion.BottomRight);
            _queue.TryTake("d", OverlayRegion.BottomRight);

            Assert.AreEqual("c", _queue.Release(OverlayRegion.BottomRight));
            Assert.AreEqual("d", _queue.Release(OverlayRegion.BottomRight));
            Assert.IsNull(_queue.Release(OverlayRegion.BottomRight));
        }

        [TestMethod]
        public void RegionsQueueIndependently()
        {
            _queue.TryTake("a", OverlayRegion.BottomRight);
            _queue.TryTake("b", OverlayRegion.BottomRight);

            // the system feed is full, the announcement region is not
            Assert.IsTrue(_queue.TryTake("city", OverlayRegion.TopCenter));
        }

        [TestMethod]
        public void SlotFreedByReleaseCanBeTakenAgain()
        {
            _queue.TryTake("a", OverlayRegion.BottomRight);
            _queue.TryTake("b", OverlayRegion.BottomRight);
            Assert.IsNull(_queue.Release(OverlayRegion.BottomRight)); // nothing queued, just frees the slot

            Assert.IsTrue(_queue.TryTake("c", OverlayRegion.BottomRight));
        }

        [TestMethod]
        public void ClearDropsBacklogAndSlots()
        {
            _queue.TryTake("a", OverlayRegion.BottomRight);
            _queue.TryTake("b", OverlayRegion.BottomRight);
            _queue.TryTake("queued", OverlayRegion.BottomRight);

            _queue.Clear();

            Assert.IsNull(_queue.Release(OverlayRegion.BottomRight));
            Assert.IsTrue(_queue.TryTake("fresh", OverlayRegion.BottomRight));
        }
    }
}
