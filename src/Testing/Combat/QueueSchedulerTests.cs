namespace LastBreathTest.Combat
{
    using Battle.Source;
    using Core.Entity;
    using Core.Entity.Components;
    using Moq;

    [TestClass]
    public class QueueSchedulerTests
    {
        [TestMethod]
        public void FastestFightersActFirst()
        {
            var scheduler = new QueueScheduler();
            var slow = Fighter(dexterity: 10);
            var fast = Fighter(dexterity: 90);
            var medium = Fighter(dexterity: 40);

            var order = scheduler.AddFighters([slow, fast, medium]);

            CollectionAssert.AreEqual(new[] { fast, medium, slow }, order.ToArray());
            scheduler.TryGetNextFighter(out var first);
            Assert.AreSame(fast, first);
        }

        [TestMethod]
        public void DeadFightersNeverEnterTheRound()
        {
            var scheduler = new QueueScheduler();
            var alive = Fighter(dexterity: 10);
            var dead = Fighter(dexterity: 90, isAlive: false);

            var order = scheduler.AddFighters([alive, dead]);

            CollectionAssert.AreEqual(new[] { alive }, order.ToArray());
        }

        [TestMethod]
        public void RefillWaitsForTheRoundToExhaust()
        {
            var scheduler = new QueueScheduler();
            var first = Fighter(dexterity: 20);
            var second = Fighter(dexterity: 10);
            scheduler.AddFighters([first, second]);

            Assert.AreEqual(0, scheduler.RefillIfEmpty([first, second]).Count); // round still running

            scheduler.TryGetNextFighter(out _);
            scheduler.TryGetNextFighter(out _);
            Assert.AreEqual(2, scheduler.RefillIfEmpty([first, second]).Count); // new round
        }

        [TestMethod]
        public void RefillRefusesWithoutTwoFighters()
        {
            var scheduler = new QueueScheduler();
            var lone = Fighter(dexterity: 20);

            Assert.AreEqual(0, scheduler.RefillIfEmpty([lone]).Count);
            Assert.IsFalse(scheduler.TryGetNextFighter(out _));
        }

        private static IFightable Fighter(int dexterity, bool isAlive = true)
        {
            var attribute = new Mock<IEntityAttribute>();
            attribute.SetupGet(a => a.Total).Returns(dexterity);
            var fighter = new Mock<IFightable>();
            fighter.SetupGet(f => f.IsAlive).Returns(isAlive);
            fighter.SetupGet(f => f.Dexterity).Returns(attribute.Object);
            return fighter.Object;
        }
    }
}
