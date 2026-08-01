namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Save.Participants;
    using Core.Services;
    using Moq;

    [TestClass]
    public class SaveLoadFlowTests
    {
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

            new AbilityBookSaveParticipant(AccessorFor(targetBook)).Restore(captured, 1);

            Assert.AreEqual("Ability_A", targetBook.GetSlotLayout(Stance.Dexterity)[0]?.Id);
            Assert.IsNull(targetBook.GetSlotLayout(Stance.Dexterity)[3]);
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
            ability.SetupGet(a => a.CurrentUpgrades).Returns(new Dictionary<int, IAbilityUpgrade>());
            return ability.Object;
        }
    }
}
