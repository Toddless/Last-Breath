namespace LastBreathTest.BattleSystemTests
{
    using Core.Ai.World.Time;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.TradeData;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Save;
    using Core.Save.Participants;
    using Core.Services;
    using Core.Session;
    using Core.Trade;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>Hybrid trader stock: authored catalog entries roll per restock, purchases decrement until
    /// the next one. The shelf is saved whole, so a reload is not a way to shop for a better roll.</summary>
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

        [TestMethod]
        public void SavedShelf_ComesBackWhole_DownToTheRolledInstanceAndTheNumbering()
        {
            var clock = Clock();
            var minted = Chest("Random_Chest", 210f);
            var shop = Shop(clock, minted);
            var stock = shop.Service.GetStock(TraderId);

            var authored = stock.Single(offer => !offer.IsRandomEquip);
            var random = stock.Single(offer => offer.IsRandomEquip);
            Assert.IsNotNull(shop.Service.TakeMany(TraderId, authored.OfferId, 1), "the walk needs a shelf already shopped at");
            shop.Service.AddBuyback(TraderId, Resource("Crafting_Resource_Sold"), amount: 4, unitPrice: 12);
            shop.Service.AddBuyback(TraderId, Chest("Iron_Chest", 55f), amount: 1, unitPrice: 250);
            shop.Service.AddBuyback(TraderId, AugmentItem(Copy()), amount: 1, unitPrice: 70);

            var loaded = Shop(clock, Chest("Random_Chest", 999f)); // a fresh mint the load must not reach for
            Restore(loaded, Written(shop));
            var restored = loaded.Service.GetStock(TraderId);

            Assert.AreEqual(authored.Remaining - 1, restored.Single(offer => offer.OfferId == authored.OfferId).Remaining,
                "what was bought before saving must stay bought");
            Assert.AreEqual(Signature(random.Item), Signature(restored.Single(offer => offer.OfferId == random.OfferId).Item),
                "a random slot must come back the instance the player was looking at, not a fresh roll");
            Assert.AreEqual(12, restored.Single(offer => offer.Item.Id == "Crafting_Resource_Sold").BuybackUnitPrice);
            Assert.AreEqual(4, restored.Single(offer => offer.Item.Id == "Crafting_Resource_Sold").Remaining);
            Assert.AreEqual(250, restored.Single(offer => offer.Item.Id == "Iron_Chest").BuybackUnitPrice);
            Assert.AreEqual(Rarity.Rare, ((IAugmentItem)restored.Single(offer => offer.Item is IAugmentItem).Item).Augment.Rarity);
            Assert.AreEqual(3, restored.Count(offer => offer.IsBuyback), "all three shapes of sold goods must survive the trip");

            loaded.Service.AddBuyback(TraderId, Resource("Crafting_Resource_Late"), amount: 1, unitPrice: 5);
            string minted_id = loaded.Service.GetStock(TraderId).Single(offer => offer.Item.Id == "Crafting_Resource_Late").OfferId;
            CollectionAssert.DoesNotContain(restored.Select(offer => offer.OfferId).ToList(), minted_id,
                "numbering must resume past the restored offers instead of colliding with them");
        }

        [TestMethod]
        public void LoadedShelf_DoesNotRestockUntilItsOwnGameTimeIsDue()
        {
            var clock = Clock();
            var shop = Shop(clock, Chest("Random_Chest", 210f));
            var before = shop.Service.GetStock(TraderId).Select(offer => offer.OfferId).ToList();

            var loaded = Shop(clock, Chest("Random_Chest", 999f));
            Restore(loaded, Written(shop));

            CollectionAssert.AreEquivalent(before, loaded.Service.GetStock(TraderId).Select(offer => offer.OfferId).ToList(),
                "a shelf whose restock is still ahead must be handed over untouched");

            clock.SetupGet(c => c.Day).Returns(1); // past the 1440-minute deadline the file carried
            var refreshed = loaded.Service.GetStock(TraderId);
            Assert.IsFalse(refreshed.Any(offer => before.Contains(offer.OfferId)), "the due date is kept, not the shelf");
        }

        [TestMethod]
        public void RestoredRandomSlot_IsStillSoldAsTheInstanceStandingOnTheShelf()
        {
            var clock = Clock();
            var shop = Shop(clock, Chest("Random_Chest", 210f));
            shop.Service.GetStock(TraderId);

            var loaded = Shop(clock, Chest("Random_Chest", 999f));
            Restore(loaded, Written(shop));
            var random = loaded.Service.GetStock(TraderId).Single(offer => offer.IsRandomEquip);

            Assert.AreEqual(random.Item.InstanceId, loaded.Service.TakeMany(TraderId, random.OfferId, 1)?.InstanceId,
                "a random slot hands over the piece it displayed — a copy would be a different thing than the one priced");
        }

        [TestMethod]
        public void SoldOutOffer_IsNotWrittenDownAtAll()
        {
            var clock = Clock();
            var shop = Shop(clock, Chest("Random_Chest", 210f));
            var random = shop.Service.GetStock(TraderId).Single(offer => offer.IsRandomEquip);
            Assert.IsNotNull(shop.Service.TakeMany(TraderId, random.OfferId, 1), "the walk needs a slot that has left the shelf");

            var offers = Written(shop)["traders"]!.Single()["offers"]!;

            Assert.IsFalse(offers.Any(offer => offer["offerId"]!.Value<string>() == random.OfferId),
                "the bought piece is in the bag now — a file naming it on the shelf too would describe it twice");
        }

        [TestMethod]
        public void ShelfNeverStocked_KeepsItsBuybackAndStocksOnTheFirstVisitAfterLoading()
        {
            var clock = Clock();
            var shop = Shop(clock, Chest("Random_Chest", 210f));
            shop.Service.AddBuyback(TraderId, Resource("Crafting_Resource_Sold"), amount: 2, unitPrice: 9);

            var loaded = Shop(clock, Chest("Random_Chest", 210f));
            Restore(loaded, Written(shop));
            var stock = loaded.Service.GetStock(TraderId);

            Assert.AreEqual(2, stock.Single(offer => offer.IsBuyback).Remaining, "a sale into an unvisited shelf must survive");
            Assert.AreEqual(2, stock.Count(offer => !offer.IsBuyback), "the first visit still stocks a shelf nobody had opened");
        }

        [TestMethod]
        public void FileWithoutTheSection_LeavesAFreshShelf()
        {
            var clock = Clock();
            var shop = Shop(clock, Chest("Random_Chest", 210f));
            var sold = shop.Service.GetStock(TraderId).Single(offer => !offer.IsRandomEquip);
            Assert.IsNotNull(shop.Service.TakeMany(TraderId, sold.OfferId, 3));
            shop.Service.AddBuyback(TraderId, Resource("Crafting_Resource_Sold"), amount: 2, unitPrice: 9);

            var scope = new LoadScope();
            var reset = new SessionResetService(scope);
            reset.Register(shop.Service);
            var manager = new SaveManager(scope, () => reset);
            manager.Register(shop.Participant);
            manager.Restore(new SaveFile());

            var stock = shop.Service.GetStock(TraderId);
            Assert.IsFalse(stock.Any(offer => offer.IsBuyback), "a file that says nothing about shelves must not carry one over");
            Assert.AreEqual(3, stock.Single(offer => !offer.IsRandomEquip).Remaining, "a shelf nobody saved is a shelf nobody shopped at");
        }

        [TestMethod]
        public void ShelvesRestoreAfterTheClockTheyAreDatedBy() =>
            Assert.IsTrue(Core.Save.RestoreOrder.TraderShelf > Core.Save.RestoreOrder.World,
                "restock deadlines are compared against the world clock, which has to be the restored one");

        /// <summary>Through real JSON text, not the object graph: the far-negative restock floor and the
        /// rolled numbers have to survive being written down.</summary>
        private static JToken Written(TraderShop shop) =>
            JToken.Parse(shop.Participant.Capture().ToString(Formatting.None));

        private static void Restore(TraderShop shop, JToken saved) =>
            shop.Participant.Restore(saved, shop.Participant.Version);

        /// <summary>What the player was looking at, in the terms the shelf shows him: the piece and its
        /// rolled lines. Two mints of one blueprint differ here and nowhere in their ids.</summary>
        private static string Signature(IItem item) => item is not IEquipItem equip
            ? $"{item.Id}"
            : $"{equip.Id}|{equip.Rarity}|{string.Join(',', equip.Modifiers.Select(line => $"{line.EntityParameter}:{line.Value}"))}";

        private static Mock<IWorldClock> Clock()
        {
            var clock = new Mock<IWorldClock>();
            clock.SetupGet(c => c.Day).Returns(0);
            clock.SetupGet(c => c.MinuteOfDay).Returns(0);
            return clock;
        }

        /// <summary>A trader service and the save participant over it, built the way the project builds
        /// them — the same converters the bag writes its own items with.</summary>
        private static TraderShop Shop(Mock<IWorldClock> clock, IEquipItem randomMint)
        {
            var traders = new Mock<ITraderProvider>();
            traders.Setup(p => p.GetTrader(TraderId)).Returns(Definition(catalogCount: 3, randomEquip: 1));

            var config = new Mock<ITradeConfigProvider>();
            config.SetupGet(c => c.Config).Returns(new TradeConfig());

            var itemData = ItemData();
            var creation = new Mock<IItemCreationService>();
            creation.Setup(c => c.CreateItem(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<Rarity>(),
                    It.IsAny<float>(), It.IsAny<float>(), It.IsAny<Rarity?>()))
                .Returns(randomMint);

            var service = new TraderService(traders.Object, config.Object, itemData.Object, creation.Object, clock.Object);
            var participant = new TraderShelfSaveParticipant(
                service, itemData.Object, new EquipItemSaveConverter(new GrantFactory(() => null, () => null, () => null)), Minter());
            return new TraderShop(service, participant);
        }

        private static Mock<IItemDataProvider> ItemData()
        {
            var itemData = new Mock<IItemDataProvider>();
            itemData.Setup(d => d.GetBlueprint(It.IsAny<string>())).Returns((EquipItemBlueprint?)null);
            itemData.SetupGet(d => d.AllBlueprints).Returns([
                new EquipItemBlueprint { Id = "Random_Chest", Piece = EquipmentPiece.Body, Rarity = Rarity.Rare }
            ]);
            itemData.Setup(d => d.CopyItem(It.IsAny<string>())).Returns((string id) => Resource(id));
            return itemData;
        }

        /// <summary>The augment seam a shelf goes through: the copy's numbers come back untouched, never
        /// drawn again.</summary>
        private static IAugmentItemMinter Minter()
        {
            var minter = new Mock<IAugmentItemMinter>();
            minter.Setup(m => m.Remembered(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, float>>(), It.IsAny<Rarity?>(), It.IsAny<string>()))
                .Returns((string id, IReadOnlyDictionary<string, float> values, Rarity? rarity, string effect) =>
                    new AugmentInstance(id, values, rarity ?? Rarity.Common, effect));
            minter.Setup(m => m.Restore(It.IsAny<AugmentInstance>()))
                .Returns((AugmentInstance copy) => AugmentItem(copy));
            return minter.Object;
        }

        private static AugmentInstance Copy() =>
            new("Augment_Test", new Dictionary<string, float> { ["Power"] = 7f }, Rarity.Rare, "Effect_Test");

        private static IAugmentItem AugmentItem(AugmentInstance copy)
        {
            var item = new Mock<IAugmentItem>();
            item.SetupGet(i => i.Augment).Returns(copy);
            item.SetupGet(i => i.Id).Returns(copy.AugmentId);
            item.SetupGet(i => i.MaxStackSize).Returns(1);
            return item.Object;
        }

        private static IItem Resource(string id) =>
            new CraftingResource(id, 999, [], Mock.Of<Core.Crafting.IMaterial>(), Rarity.Common);

        private static IEquipItem Chest(string id, float armor)
        {
            var item = new EquipItem(EquipmentPiece.Body, id, ["armor"]) { Rarity = Rarity.Rare };
            item.SetModifiers([new Core.Modifiers.SimpleModifier(EntityParameter.Armor, ModifierValueType.Flat, armor, item.InstanceId)]);
            return item;
        }

        private sealed record TraderShop(TraderService Service, TraderShelfSaveParticipant Participant);

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

        private static TraderDefinition Definition(int catalogCount = 1, int randomEquip = 0) => new(
            TraderId,
            Fractions.Human,
            RestockGameMinutes: 1440f,
            Catalog: [new TraderCatalogEntryData { ItemId = "Crafting_Resource_Test", Count = catalogCount }],
            RandomEquip: randomEquip > 0 ? new TraderRandomEquipData { Count = randomEquip } : null);
    }
}
