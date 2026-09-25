namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Crafting.Source.EventHandlers;
    using Moq;

    /// <summary>Shattering refunds from BOTH used-resource parts — the recipe's required resources
    /// and the optional additives — scaled by the mastery refund fraction alike. EVERY shattered
    /// equip additionally yields the dust of its category AND rarity tier (Common falls into the
    /// Uncommon tier, Unique into Legendary): flat 1 pile, grown only by the raw resource-return
    /// channel — floor(1 × (1 + bonus)).</summary>
    [TestClass]
    public class DestroyItemRefundTests
    {
        [TestMethod]
        public async Task Shatter_RefundsRequiredAndOptionalParts_ScaledByMastery_PlusDust()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []) { Rarity = Rarity.Common };
            item.SaveUsedResources(
                new Dictionary<string, int> { ["Iron"] = 4 },
                new Dictionary<string, int> { ["Essence"] = 2 });

            var inventory = InventoryWith(item);
            var mastery = new Mock<ICraftingMastery>();
            mastery.Setup(mock => mock.GetCurrentResourceMultiplier(It.IsAny<float>())).Returns(0.5f);

            await Handle(inventory, mastery, item);

            inventory.Verify(mock => mock.TryAddItemStacks("Iron", 2), Times.Once);    // required part, halved
            inventory.Verify(mock => mock.TryAddItemStacks("Essence", 1), Times.Once); // optional part, halved
            inventory.Verify(mock => mock.TryAddItemStacks("Upgrade_Resource_Jewellery_Dust_Uncommon", 1), Times.Once); // crafted items dust too; Common shatters into the Uncommon tier
            inventory.Verify(mock => mock.RemoveItemByInstanceId(item.InstanceId), Times.Once);
        }

        [TestMethod]
        public async Task Shatter_LootItem_YieldsOnlyOneDust_AtZeroMastery()
        {
            // A drop remembers no resources: dust is its WHOLE return, and the base is exactly 1
            // regardless of rarity (mastery mock defaults every channel to 0).
            var item = new EquipItem(EquipmentPiece.Ring, "Loot_Band", []) { Rarity = Core.Enums.Rarity.Legendary };
            var inventory = InventoryWith(item);

            await Handle(inventory, new Mock<ICraftingMastery>(), item);

            inventory.Verify(mock => mock.TryAddItemStacks("Upgrade_Resource_Jewellery_Dust_Legendary", 1), Times.Once); // legendary gear pays legendary dust
            inventory.Verify(mock => mock.TryAddItemStacks(It.IsAny<string>(), It.IsAny<int>()), Times.Once, "Nothing but the dust may come back from loot.");
            inventory.Verify(mock => mock.RemoveItemByInstanceId(item.InstanceId), Times.Once);
        }

        [TestMethod]
        public async Task Shatter_DustGrowsWithTheRawReturnChannel_Floored()
        {
            // floor(1 × (1 + 1.6)) = 2 — the raw channel bonus, NOT the refund fraction, drives dust.
            var item = new EquipItem(EquipmentPiece.Weapon, "Loot_Blade", []) { Rarity = Rarity.Rare };
            var inventory = InventoryWith(item);
            var mastery = new Mock<ICraftingMastery>();
            mastery.Setup(mock => mock.GetResourceReturnBonus()).Returns(1.6f);

            await Handle(inventory, mastery, item);

            inventory.Verify(mock => mock.TryAddItemStacks("Upgrade_Resource_Weapon_Dust_Rare", 2), Times.Once);
        }

        private static Mock<IInventory> InventoryWith(EquipItem item)
        {
            var inventory = new Mock<IInventory>();
            inventory.Setup(mock => mock.GetItem<IEquipItem>(item.InstanceId)).Returns(item);
            inventory.Setup(mock => mock.TryAddItemStacks(It.IsAny<string>(), It.IsAny<int>())).Returns(true);
            return inventory;
        }

        private static Task Handle(Mock<IInventory> inventory, Mock<ICraftingMastery> mastery, EquipItem item)
        {
            var handler = new DestroyItemMessageHandler(
                inventory.Object, Mock.Of<IItemDataProvider>(), mastery.Object, Mock.Of<IGameMessageBus>());
            return handler.HandleMessageAsync(new DestroyItemMessage(item.InstanceId));
        }
    }
}
