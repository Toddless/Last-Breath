namespace LastBreathTest.CraftingSystem
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Crafting.Source;
    using Crafting.Source.RequestHandlers;
    using Moq;

    /// <summary>The growing recraft price: every SUCCESSFUL reroll bumps the item's counter and the
    /// next price is ceil(base × (1 + 0.1 × count)). The handler is the price authority — it computes
    /// and spends that price itself (the request carries only additives), and a refused reroll costs
    /// nothing and moves no counter.</summary>
    [TestClass]
    public class RecraftPriceTests
    {
        [TestMethod]
        public void GetRecraftResourceCost_GrowsByCeilTenPercentPerReroll()
        {
            var upgrader = CreateUpgrader(seed: 1, CostProvider(("Dust", 1), ("Ore", 10)));
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);

            foreach ((int rerolls, int expectedDust, int expectedOre) in
                     (ReadOnlySpan<(int, int, int)>)[(0, 1, 10), (1, 2, 11), (2, 2, 12), (5, 2, 15), (10, 2, 20)])
            {
                item.RecraftCount = rerolls;
                var cost = upgrader.GetRecraftResourceCost(item);
                Assert.AreEqual(expectedDust, cost.Single(entry => entry.Id == "Dust").Amount, $"Dust after {rerolls} rerolls");
                Assert.AreEqual(expectedOre, cost.Single(entry => entry.Id == "Ore").Amount, $"Ore after {rerolls} rerolls");
            }
        }

        [TestMethod]
        public async Task Handler_ComputesAndSpendsTheItemPrice_RequestCarriesOnlyAdditives()
        {
            // The item already survived two rerolls: the handler must charge ceil(1 × 1.2) = 2 dust
            // by itself — the request brings nothing but an essence.
            (var item, var provider) = RerollableItem();
            item.RecraftCount = 2;
            var inventory = InventoryWith(item, amountOfEverything: 99);
            var upgrader = CreateUpgrader(seed: 42, provider);
            var handler = new RecraftEquipItemModifierRequestHandler(
                inventory.Object, upgrader, new CraftingResources(inventory.Object), Mock.Of<IGameMessageBus>());

            var result = await handler.HandleRequest(new RecraftEquipItemModifierRequest(
                item.InstanceId, item.Modifiers.Single().InstanceId,
                AdditiveResources: new Dictionary<string, int> { ["Essence"] = 1 }));

            Assert.IsTrue(result.IsSuccess, result.Message);
            inventory.Verify(mock => mock.RemoveItemById("Dust", 2), Times.Once, "The handler must spend ITS computed price.");
            inventory.Verify(mock => mock.RemoveItemById("Essence", 1), Times.Once, "The request's additives are spent alongside.");
            Assert.AreEqual(3, item.RecraftCount, "The successful reroll must bump the counter for the NEXT price.");
        }

        [TestMethod]
        public async Task Handler_RefusedReroll_SpendsNothingAndKeepsTheCounter()
        {
            // No pool anywhere: the reroll refuses — free, and the growing-price counter stays put.
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix };
            item.AddAdditionalModifier(line);
            var inventory = InventoryWith(item, amountOfEverything: 99);
            var upgrader = CreateUpgrader(seed: 42, CostProvider(("Dust", 1)));
            var handler = new RecraftEquipItemModifierRequestHandler(
                inventory.Object, upgrader, new CraftingResources(inventory.Object), Mock.Of<IGameMessageBus>());

            var result = await handler.HandleRequest(new RecraftEquipItemModifierRequest(item.InstanceId, line.InstanceId, AdditiveResources: []));

            Assert.IsFalse(result.IsSuccess);
            inventory.Verify(mock => mock.RemoveItemById(It.IsAny<string>(), It.IsAny<int>()), Times.Never, "A refusal must be free.");
            Assert.AreEqual(0, item.RecraftCount, "A refusal must not move the counter.");
        }

        [TestMethod]
        public async Task Handler_PricesBeforeTheReroll_TheBumpAffectsOnlyTheNextOne()
        {
            // First reroll on a fresh item: base price (1 dust) is spent even though the counter is 1 afterwards.
            (var item, var provider) = RerollableItem();
            var inventory = InventoryWith(item, amountOfEverything: 99);
            var upgrader = CreateUpgrader(seed: 7, provider);
            var handler = new RecraftEquipItemModifierRequestHandler(
                inventory.Object, upgrader, new CraftingResources(inventory.Object), Mock.Of<IGameMessageBus>());

            var result = await handler.HandleRequest(new RecraftEquipItemModifierRequest(
                item.InstanceId, item.Modifiers.Single().InstanceId, AdditiveResources: []));

            Assert.IsTrue(result.IsSuccess, result.Message);
            inventory.Verify(mock => mock.RemoveItemById("Dust", 1), Times.Once, "The FIRST reroll pays the base price.");
            Assert.AreEqual(1, item.RecraftCount);
        }

        /// <summary>A ring with one suffix line and a provider whose item pool can serve the reroll
        /// AND whose recraft cost is 1 Dust.</summary>
        private static (EquipItem Item, IItemDataProvider Provider) RerollableItem()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix };
            item.AddAdditionalModifier(line);

            var provider = ProviderWithCost(("Dust", 1));
            provider.Setup(mock => mock.GetEquipItemModifierPool("Band")).Returns(
            [
                new ParameterDescriptor(EntityParameter.Strength, ModifierValueType.Flat, 10f, ModifierScope.Global) { Weight = 10f, Affix = AffixKind.Suffix },
            ]);
            return (item, provider.Object);
        }

        private static Mock<IInventory> InventoryWith(EquipItem item, int amountOfEverything)
        {
            var inventory = new Mock<IInventory>();
            inventory.Setup(mock => mock.GetItem<IEquipItem>(item.InstanceId)).Returns(item);
            inventory.Setup(mock => mock.GetTotalItemAmount(It.IsAny<string>())).Returns(amountOfEverything);
            return inventory;
        }

        private static ItemUpgrader CreateUpgrader(int seed, IItemDataProvider provider)
        {
            var rnd = new DefaultRandomNumberGenerator(seed);
            return new ItemUpgrader(rnd, Mock.Of<ICraftingMastery>(), provider,
                Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(rnd));
        }

        private static IItemDataProvider CostProvider(params (string Id, int Amount)[] requirements) =>
            ProviderWithCost(requirements).Object;

        /// <summary>Provider with honest empty pools everywhere and a fixed recraft base cost.</summary>
        private static Mock<IItemDataProvider> ProviderWithCost(params (string Id, int Amount)[] requirements)
        {
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetEquipItemBaseModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetResourceDescriptors(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetRecraftCost(It.IsAny<EquipmentCategory>(), It.IsAny<Rarity>()))
                .Returns(requirements.Select(IRequirement (entry) => new Requirement(RequirementType.Resource, entry.Id, entry.Amount)).ToList());
            return provider;
        }
    }
}
