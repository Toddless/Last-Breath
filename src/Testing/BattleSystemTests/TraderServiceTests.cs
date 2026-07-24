namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.TradeData;
    using Core.Enums;
    using Core.Items;
    using Core.Trade;
    using Moq;

    /// <summary>Hybrid trader stock (Todd 2026-07-24): authored catalog entries roll per restock,
    /// purchases decrement until the next restock; stock is session state (not saved yet).</summary>
    [TestClass]
    public class TraderServiceTests
    {
        private const string TraderId = "Trader_Test";

        [TestMethod]
        public void CatalogEntry_StocksAndDecrementsUntilGone()
        {
            var service = Service(Definition(catalogCount: 2));

            var stock = service.GetStock(TraderId);
            Assert.AreEqual(1, stock.Count);
            Assert.AreEqual(2, stock[0].Remaining);

            Assert.IsNotNull(service.TakeOne(TraderId, stock[0].OfferId));
            Assert.IsNotNull(service.TakeOne(TraderId, stock[0].OfferId));
            Assert.IsNull(service.TakeOne(TraderId, stock[0].OfferId), "an emptied offer must refuse");
            Assert.AreEqual(0, service.GetStock(TraderId).Count, "sold out = an empty shelf until restock");
        }

        [TestMethod]
        public void PlainGoods_HandOutCopies_NotTheTemplate()
        {
            var service = Service(Definition(catalogCount: 2));
            var offer = service.GetStock(TraderId)[0];

            var first = service.TakeOne(TraderId, offer.OfferId);
            var second = service.TakeOne(TraderId, offer.OfferId);

            Assert.AreNotSame(first, second, "every purchase must mint its own copy");
            Assert.AreNotEqual(first!.InstanceId, second!.InstanceId);
        }

        [TestMethod]
        public void UnknownTrader_HasEmptyShelf()
        {
            var service = Service(Definition());
            Assert.AreEqual(0, service.GetStock("Trader_Nobody").Count);
        }

        [TestMethod]
        public void SessionReset_ForgetsShelvesAndTimers()
        {
            var service = Service(Definition(catalogCount: 1));
            var offer = service.GetStock(TraderId)[0];
            Assert.IsNotNull(service.TakeOne(TraderId, offer.OfferId));
            Assert.AreEqual(0, service.GetStock(TraderId).Count);

            service.ResetSession();

            Assert.AreEqual(1, service.GetStock(TraderId).Count, "a new game restocks from scratch");
        }

        private static TraderService Service(TraderDefinition definition)
        {
            var traders = new Mock<ITraderProvider>();
            traders.Setup(p => p.GetTrader(definition.Id)).Returns(definition);

            var config = new Mock<ITradeConfigProvider>();
            config.SetupGet(c => c.Config).Returns(new TradeConfig());

            var itemData = new Mock<Core.Data.IItemDataProvider>();
            itemData.Setup(d => d.GetBlueprint(It.IsAny<string>())).Returns((EquipItemBlueprint?)null);
            itemData.Setup(d => d.CopyItem(It.IsAny<string>()))
                .Returns((string id) => new CraftingResource(id, 999, [], Mock.Of<Core.Crafting.IMaterial>(), Rarity.Common));

            return new TraderService(traders.Object, config.Object, itemData.Object);
        }

        private static TraderDefinition Definition(int catalogCount = 1) => new(
            TraderId,
            Fractions.Human,
            RestockGameMinutes: 1440f,
            Catalog: [new TraderCatalogEntryData { ItemId = "Crafting_Resource_Test", Count = catalogCount }],
            RandomEquip: null);
    }
}
