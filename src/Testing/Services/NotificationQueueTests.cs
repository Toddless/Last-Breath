namespace LastBreathTest.Services
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
            Assert.IsTrue(_queue.TryTake(Note("a"), OverlayRegion.BottomRight));
            Assert.IsTrue(_queue.TryTake(Note("b"), OverlayRegion.BottomRight));
            Assert.IsFalse(_queue.TryTake(Note("c"), OverlayRegion.BottomRight));
        }

        [TestMethod]
        public void ReleaseHandsOutQueuedNotificationsInFifoOrder()
        {
            _queue.TryTake(Note("a"), OverlayRegion.BottomRight);
            _queue.TryTake(Note("b"), OverlayRegion.BottomRight);
            _queue.TryTake(Note("c"), OverlayRegion.BottomRight);
            _queue.TryTake(Note("d"), OverlayRegion.BottomRight);

            Assert.AreEqual("c", _queue.Release(OverlayRegion.BottomRight)?.Id);
            Assert.AreEqual("d", _queue.Release(OverlayRegion.BottomRight)?.Id);
            Assert.IsNull(_queue.Release(OverlayRegion.BottomRight));
        }

        [TestMethod]
        public void RegionsQueueIndependently()
        {
            _queue.TryTake(Note("a"), OverlayRegion.BottomRight);
            _queue.TryTake(Note("b"), OverlayRegion.BottomRight);

            // the system feed is full, the announcement region is not
            Assert.IsTrue(_queue.TryTake(Note("city"), OverlayRegion.TopCenter));
        }

        [TestMethod]
        public void SlotFreedByReleaseCanBeTakenAgain()
        {
            _queue.TryTake(Note("a"), OverlayRegion.BottomRight);
            _queue.TryTake(Note("b"), OverlayRegion.BottomRight);
            Assert.IsNull(_queue.Release(OverlayRegion.BottomRight)); // nothing queued, just frees the slot

            Assert.IsTrue(_queue.TryTake(Note("c"), OverlayRegion.BottomRight));
        }

        [TestMethod]
        public void ClearDropsBacklogAndSlots()
        {
            _queue.TryTake(Note("a"), OverlayRegion.BottomRight);
            _queue.TryTake(Note("b"), OverlayRegion.BottomRight);
            _queue.TryTake(Note("queued"), OverlayRegion.BottomRight);

            _queue.Clear();

            Assert.IsNull(_queue.Release(OverlayRegion.BottomRight));
            Assert.IsTrue(_queue.TryTake(Note("fresh"), OverlayRegion.BottomRight));
        }

        private static NotificationContent Note(string id) => new(id);
    }
}
