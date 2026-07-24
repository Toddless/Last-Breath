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

            Assert.IsNotNull(service.TakeMany(TraderId, stock[0].OfferId, 1));
            Assert.IsNotNull(service.TakeMany(TraderId, stock[0].OfferId, 1));
            Assert.IsNull(service.TakeMany(TraderId, stock[0].OfferId, 1), "an emptied offer must refuse");
            Assert.AreEqual(0, service.GetStock(TraderId).Count, "sold out = an empty shelf until restock");
        }

        [TestMethod]
        public void PlainGoods_HandOutCopies_NotTheTemplate()
        {
            var service = Service(Definition(catalogCount: 2));
            var offer = service.GetStock(TraderId)[0];

            var first = service.TakeMany(TraderId, offer.OfferId, 1);
            var second = service.TakeMany(TraderId, offer.OfferId, 1);

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
            Assert.IsNotNull(service.TakeMany(TraderId, offer.OfferId, 1));
            Assert.AreEqual(0, service.GetStock(TraderId).Count);

            service.ResetSession();

            Assert.AreEqual(1, service.GetStock(TraderId).Count, "a new game restocks from scratch");
        }

        [TestMethod]
        public void PartialAmount_IsAllOrNothing()
        {
            var service = Service(Definition(catalogCount: 3));
            var offer = service.GetStock(TraderId)[0];

            Assert.IsNull(service.TakeMany(TraderId, offer.OfferId, 5), "an amount over the shelf must refuse whole");
            Assert.AreEqual(3, service.GetStock(TraderId)[0].Remaining, "the refused take must not touch the shelf");
            Assert.IsNotNull(service.TakeMany(TraderId, offer.OfferId, 3));
        }

        [TestMethod]
        public void SoldItem_WaitsOnTheBuybackShelf_AtTheEarnedPrice()
        {
            var service = Service(Definition(catalogCount: 1));
            var sold = new CraftingResource("Crafting_Resource_Sold", 999, [], Mock.Of<Core.Crafting.IMaterial>(), Rarity.Rare);

            service.AddBuyback(TraderId, sold, amount: 4, unitPrice: 12);

            var buyback = service.GetStock(TraderId).Single(offer => offer.IsBuyback);
            Assert.AreEqual(4, buyback.Remaining);
            Assert.AreEqual(12, buyback.BuybackUnitPrice, "undoing a sale must cost exactly what it earned");

            Assert.IsNotNull(service.TakeMany(TraderId, buyback.OfferId, 4));
            Assert.IsFalse(service.GetStock(TraderId).Any(offer => offer.IsBuyback), "an emptied buyback offer leaves the shelf");
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
