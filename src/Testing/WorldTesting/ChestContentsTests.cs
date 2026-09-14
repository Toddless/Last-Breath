namespace LastBreathTest.WorldTesting
{
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.World.Containers;
    using Moq;

    [TestClass]
    public class ChestContentsTests
    {
        private static IItem Item(string id)
        {
            var item = new Mock<IItem>();
            item.SetupGet(x => x.Id).Returns(id);
            return item.Object;
        }
        private static ChestDefinition Definition() => new("test", "test", 5,
            [new("first", "sword", 1), new("second", "ore", 10)]);

        private sealed class Receiver(Dictionary<string, int> capacity, Action? notify = null) : IInventoryTransfer
        {
            public readonly Dictionary<string, int> Accepted = [];
            public int ReceiveUpTo(IItem item, int amount)
            {
                int count = Math.Min(amount, capacity.GetValueOrDefault(item.Id));
                capacity[item.Id] = capacity.GetValueOrDefault(item.Id) - count;
                Accepted[item.Id] = Accepted.GetValueOrDefault(item.Id) + count;
                return count;
            }
            public IDisposable DeferNotifications() => new Scope(notify);
            private sealed class Scope(Action? action) : IDisposable { public void Dispose() => action?.Invoke(); }
        }

        [TestMethod]
        public void PartialStackAndLaterSlotsRemainAvailableAfterRefusal()
        {
            var chest = new ChestContents();
            chest.Initialize(Definition(), x => Item(x.ItemId));
            var result = chest.Transfer(new Receiver(new() { ["ore"] = 3 }), null, 100, 5, () => true);
            Assert.AreEqual(3, result.Accepted);
            Assert.IsTrue(result.CapacityLimited);
            Assert.AreEqual(1, chest.Slots[0].Amount);
            Assert.AreEqual(7, chest.Slots[1].Amount);
            Assert.AreEqual("second", chest.Slots[1].Id);
            Assert.IsNull(chest.RemoveAtMinutes);
        }

        [TestMethod]
        public void NotificationSeesCommittedSourceAndCannotReenterTransfer()
        {
            var chest = new ChestContents();
            chest.Initialize(Definition(), x => Item(x.ItemId));
            int observed = -1;
            Receiver? receiver = null;
            receiver = new Receiver(new() { ["sword"] = 1, ["ore"] = 10 }, () =>
            {
                observed = chest.Slots.Sum(x => x.Amount);
                Assert.AreEqual(105d, chest.RemoveAtMinutes);
                Assert.AreEqual(0, chest.Transfer(receiver!, null, 100, 5, () => true).Accepted);
            });
            chest.Transfer(receiver, null, 100, 5, () => true);
            Assert.AreEqual(0, observed);
            Assert.IsFalse(chest.RemovalDue(104.99));
            Assert.IsTrue(chest.RemovalDue(105));
            chest.Transfer(new Receiver(new()), null, 200, 5, () => true);
            Assert.AreEqual(105d, chest.RemoveAtMinutes);
        }

        [TestMethod]
        public void OpeningAndRestoringDoNotReroll()
        {
            var chest = new ChestContents();
            int calls = 0;
            chest.Initialize(Definition(), x => { calls++; return Item(x.ItemId); });
            var first = chest.Slots[0].Item;
            chest.Initialize(Definition(), _ => throw new Exception("Must not mint twice"));
            Assert.AreEqual(2, calls);
            Assert.AreSame(first, chest.Slots[0].Item);
            var restored = new ChestContents();
            restored.Restore(true, [new("first", first, 1), new("second", chest.Slots[1].Item, 7)], null);
            restored.Initialize(Definition(), _ => throw new Exception("Restore must not mint"));
            Assert.AreEqual(7, restored.Slots[1].Amount);
        }

        [TestMethod]
        public void FailedCreationDoesNotCommitPartialContents()
        {
            var chest = new ChestContents();
            Assert.ThrowsException<InvalidOperationException>(() => chest.Initialize(Definition(),
                x => x.ItemId == "ore" ? throw new InvalidOperationException() : Item(x.ItemId)));
            Assert.IsFalse(chest.Initialized);
            Assert.AreEqual(0, chest.Slots.Count);
        }

        [TestMethod]
        public void LostAccessAndUnknownSlotDoNotTransfer()
        {
            var chest = new ChestContents();
            chest.Initialize(Definition(), x => Item(x.ItemId));
            var receiver = new Receiver(new() { ["sword"] = 1, ["ore"] = 10 });
            Assert.AreEqual(0, chest.Transfer(receiver, null, 10, 5, () => false).Accepted);
            Assert.AreEqual(0, chest.Transfer(receiver, "absent", 10, 5, () => true).Accepted);
            Assert.AreEqual(11, chest.Slots.Sum(x => x.Amount));
        }

        [TestMethod]
        public void InvalidStateAndDefinitionAreRejected()
        {
            Assert.ThrowsException<InvalidOperationException>(() => ChestCatalog.Validate(Definition() with { EmptyRemovalDelayMinutes = double.NaN }));
            Assert.ThrowsException<InvalidOperationException>(() => ChestCatalog.Validate(Definition() with { Items = [new("same", "x", 1), new("same", "y", 1)] }));
            Assert.ThrowsException<InvalidOperationException>(() => new ChestContents().Restore(true, [new("x", null, 1)], null));
            Assert.ThrowsException<InvalidOperationException>(() => new ChestContents().Restore(true, [], null));
        }
    }
}
