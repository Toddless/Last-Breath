namespace LastBreathTest.BattleSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Inventory;
    using Core.Items;
    using Core.Modifiers;
    using Core.Results;
    using Crafting.Source;
    using Moq;

    /// <summary>
    /// The regressions stage A fixed must stay fixed: all-or-nothing spending, the max-level
    /// short-circuit before any cost, and recraft refusing instead of looping forever. Paths
    /// under test deliberately end BEFORE any roll — Godot's RNG cannot exist in this host.
    /// </summary>
    [TestClass]
    public class CraftingDomainTests
    {
        private Mock<IInventory> _inventory = null!;
        private CraftingResources _resources = null!;

        [TestInitialize]
        public void Setup()
        {
            _inventory = new Mock<IInventory>();
            _resources = new CraftingResources(_inventory.Object);
        }

        [TestMethod]
        public void TrySpend_MissingOneResource_SpendsNothing()
        {
            _inventory.Setup(mock => mock.GetTotalItemAmount("Ore")).Returns(10);
            _inventory.Setup(mock => mock.GetTotalItemAmount("Wood")).Returns(1);

            bool spent = _resources.TrySpend(new Dictionary<string, int> { ["Ore"] = 5, ["Wood"] = 2 });

            Assert.IsFalse(spent);
            _inventory.Verify(mock => mock.RemoveItemById(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void TrySpend_EverythingPresent_RemovesExactAmounts()
        {
            _inventory.Setup(mock => mock.GetTotalItemAmount(It.IsAny<string>())).Returns(10);

            bool spent = _resources.TrySpend(new Dictionary<string, int> { ["Ore"] = 5, ["Wood"] = 2 });

            Assert.IsTrue(spent);
            _inventory.Verify(mock => mock.RemoveItemById("Ore", 5), Times.Once);
            _inventory.Verify(mock => mock.RemoveItemById("Wood", 2), Times.Once);
        }

        [TestMethod]
        public void TryUpgradeItem_AtMaxLevel_ShortCircuitsBeforeAnyRoll()
        {
            var upgrader = CreateUpgrader();
            var item = new Mock<IEquipItem>();
            item.SetupGet(mock => mock.UpdateLevel).Returns(12);
            item.SetupGet(mock => mock.MaxUpdateLevel).Returns(12);

            // A null RNG proves no roll happens: reaching one would throw.
            var result = upgrader.TryUpgradeItem(item.Object);

            Assert.AreEqual(ItemUpgradeResult.ReachedMaxLevel, result);
            item.Verify(mock => mock.Upgrade(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void TryRecraftModifier_HashNotOnItem_RefusesWithoutRolling()
        {
            var upgrader = CreateUpgrader();
            var item = new Mock<IEquipItem>();
            item.SetupGet(mock => mock.Modifiers).Returns([]);

            var result = upgrader.TryRecraftModifier(item.Object, modifierToReroll: 12345, modifiers: []);

            Assert.IsNull(result);
            item.Verify(mock => mock.RemoveAdditionalModifier(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void TryRecraftModifier_ExhaustedPool_RefusesInsteadOfLooping()
        {
            var upgrader = CreateUpgrader();
            var onItem = new Mock<IModifierInstance>();
            var item = new Mock<IEquipItem>();
            item.SetupGet(mock => mock.Modifiers).Returns([onItem.Object]);
            item.SetupGet(mock => mock.ModifiersPool).Returns([]);

            // The only candidate pool is empty once the rerolled hash is excluded — the old code span forever here.
            var result = upgrader.TryRecraftModifier(item.Object, onItem.Object.GetHashCode(), modifiers: []);

            Assert.IsNull(result);
            item.Verify(mock => mock.RemoveAdditionalModifier(It.IsAny<int>()), Times.Never);
        }

        private static ItemUpgrader CreateUpgrader() =>
            new(rnd: null!, Mock.Of<ICraftingMastery>(), Mock.Of<IItemDataProvider>(),
                Mock.Of<ICraftingAdditiveProvider>());
    }
}
