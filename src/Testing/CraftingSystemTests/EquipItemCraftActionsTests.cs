namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;

    /// <summary>
    /// Tooltip button visibility (2026-07-16): the tooltip mirrors only the obvious — upgrade below
    /// the cap, recraft when there are lines to reroll, ascend only on legendaries; a sealed
    /// (ascended) item offers nothing. The deep CanAscend validation stays with the crafting window
    /// and its handlers.
    /// </summary>
    [TestClass]
    public class EquipItemCraftActionsTests
    {
        [TestMethod]
        public void UnsealedNonLegendary_ShowsUpgradeAndRecraft_ButNotAscend()
        {
            var item = WithLine(new EquipItem(EquipmentPiece.Ring, "Band", []) { Rarity = Rarity.Rare });

            Assert.IsTrue(EquipItemCraftActions.CanShowUpgrade(item));
            Assert.IsTrue(EquipItemCraftActions.CanShowRecraft(item));
            Assert.IsFalse(EquipItemCraftActions.CanShowAscend(item));
        }

        [TestMethod]
        public void UnsealedLegendary_ShowsAllThree()
        {
            var item = WithLine(new EquipItem(EquipmentPiece.Ring, "Band", []) { Rarity = Rarity.Legendary });

            Assert.IsTrue(EquipItemCraftActions.CanShowUpgrade(item));
            Assert.IsTrue(EquipItemCraftActions.CanShowRecraft(item));
            Assert.IsTrue(EquipItemCraftActions.CanShowAscend(item));
        }

        [TestMethod]
        public void DataBornMythic_PreSharpenedAndLineless_OffersNothing()
        {
            // All-Cutting shape: full level out of the mint, no rolled lines, NOT sealed.
            var item = new EquipItem(EquipmentPiece.Weapon, "All_Cutting", []) { Rarity = Rarity.Mythic, MaxUpdateLevel = 42 };
            item.Upgrade(42);

            Assert.IsFalse(EquipItemCraftActions.CanShowUpgrade(item), "At the cap there is nothing to sharpen.");
            Assert.IsFalse(EquipItemCraftActions.CanShowRecraft(item), "No lines — nothing to reroll.");
            Assert.IsFalse(EquipItemCraftActions.CanShowAscend(item));
        }

        [TestMethod]
        public void SealedItem_ShowsNothing()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []) { Rarity = Rarity.Legendary };
            item.SetModifiers([new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test")]);
            item.Upgrade(item.MaxUpdateLevel);
            Assert.IsTrue(item.TryAscend()); // the real seal path — no test backdoor

            Assert.IsFalse(EquipItemCraftActions.CanShowUpgrade(item));
            Assert.IsFalse(EquipItemCraftActions.CanShowRecraft(item));
            Assert.IsFalse(EquipItemCraftActions.CanShowAscend(item));
        }

        private static EquipItem WithLine(EquipItem item)
        {
            item.SetModifiers([new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test")]);
            return item;
        }
    }
}
