namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Entity;
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
        public void RestoringAnEarlierSaveForgetsExtraAbilities()
        {
            // Save with only Ability_A learned, then the player learns Ability_B in the same session.
            var provider = new Mock<IAbilityProvider>();
            var sourceBook = new AbilityBookComponent(new Mock<IFightable>().Object);
            sourceBook.Learn(Stance.Dexterity, FakeAbility("Ability_A"));
            var captured = new AbilityBookSaveParticipant(AccessorFor(sourceBook), provider.Object).Capture();

            var targetBook = new AbilityBookComponent(new Mock<IFightable>().Object);
            targetBook.Learn(Stance.Dexterity, FakeAbility("Ability_A"));
            targetBook.Learn(Stance.Dexterity, FakeAbility("Ability_B")); // learned after the save
            provider.SetupGet(p => p.KnownAbilityIds).Returns(["Ability_A", "Ability_B"]);

            new AbilityBookSaveParticipant(AccessorFor(targetBook), provider.Object).Restore(captured, 1);

            Assert.AreEqual("Ability_A", targetBook.GetAbilities(Stance.Dexterity).Single().Id);
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
