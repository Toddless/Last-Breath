namespace LastBreathTest.Trade
{
    using Core.Data;
    using Core.Enums;
    using Core.Items;
    using Core.Save.Participants;
    using Core.Trade;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Trade core (stage 3, decisions 2026-07-24): gold is a wallet counter; an item's gold value is
    /// the INSTANCE valuation basePrice × rarity × (1 + bonus × sharpening) × ascension. Loot-table
    /// prices are generator budget units and never enter this formula.
    /// </summary>
    [TestClass]
    public class TradeCoreTests
    {
        // ---------- valuation ----------

        [TestMethod]
        public void PlainItem_PricesFromOwnBaseAndRarity()
        {
            var valuation = new ItemValuation(Config());
            var item = new Mock<IItem>();
            item.SetupGet(i => i.BasePrice).Returns(100);
            item.SetupGet(i => i.Rarity).Returns(Rarity.Rare);

            Assert.AreEqual(250, valuation.Value(item.Object)); // 100 x 2.5
        }

        [TestMethod]
        public void EquipInstance_PricesFromBlueprintTimesRaritySharpeningAndAscension()
        {
            var blueprints = new Mock<IEquipBlueprintProvider>();
            blueprints.Setup(b => b.GetBlueprint("Weapon_Test"))
                .Returns(new EquipItemBlueprint { Id = "Weapon_Test", BasePrice = 100 });
            var valuation = new ItemValuation(Config(), blueprints.Object);

            var equip = new Mock<IEquipItem>();
            equip.SetupGet(e => e.Id).Returns("Weapon_Test");
            equip.SetupGet(e => e.Rarity).Returns(Rarity.Mythic);
            equip.SetupGet(e => e.UpdateLevel).Returns(5);
            equip.SetupGet(e => e.IsSealed).Returns(true);

            // 100 x 15 (Mythic) x 1.5 (5 levels x 0.1) x 1.5 (ascended) = 3375
            Assert.AreEqual(3375, valuation.Value(equip.Object));
        }

        [TestMethod]
        public void UnpricedItem_ValuesToZero()
        {
            var valuation = new ItemValuation(Config());
            var item = new Mock<IItem>();
            item.SetupGet(i => i.BasePrice).Returns(0);
            item.SetupGet(i => i.Rarity).Returns(Rarity.Legendary);

            Assert.AreEqual(0, valuation.Value(item.Object), "no authored base = untradable, never a guessed price");
        }

        // ---------- wallet ----------

        [TestMethod]
        public void Wallet_SpendIsAllOrNothing()
        {
            var wallet = new WalletService(Config());
            wallet.Add(100);

            Assert.IsFalse(wallet.TrySpend(150), "overspend must be refused");
            Assert.AreEqual(100, wallet.Gold, "a refused spend must not touch the balance");
            Assert.IsTrue(wallet.TrySpend(60));
            Assert.AreEqual(40, wallet.Gold);
        }

        [TestMethod]
        public void Wallet_SaveRoundtripAndSessionReset()
        {
            var config = Config(startingGold: 25);
            var wallet = new WalletService(config);
            wallet.Add(500);

            var participant = new WalletSaveParticipant(wallet);
            var captured = participant.Capture();

            wallet.ResetSession();
            Assert.AreEqual(25, wallet.Gold, "a new game starts from the configured gold");

            participant.Restore(JToken.Parse(captured.ToString()), savedVersion: 1);
            Assert.AreEqual(525, wallet.Gold, "the load must bring the saved balance back");
        }

        private static ITradeConfigProvider Config(int startingGold = 0)
        {
            var provider = new Mock<ITradeConfigProvider>();
            provider.SetupGet(p => p.Config).Returns(new TradeConfig { StartingGold = startingGold });
            return provider.Object;
        }
    }
}
