namespace LastBreathTest.LootSimulation
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using LootGeneration.Internal;
    using Moq;

    /// <summary>The loot drop's bonus-grant roll: NPC signature effects (AdditionalItemEffects from
    /// ItemEffectsModifier) guarantee one grant per equip item; ordinary kills roll the configured
    /// chance against the whole ItemEffects catalog. Fixed rarities (Unique/Mythic) are untouched.</summary>
    [TestClass]
    public class LootGrantRollTests
    {
        private const string ItemId = "Test_Item";

        [TestMethod]
        public void SignatureEffects_GuaranteeOneGrantFromTheListedIds()
        {
            var service = CreateService(Catalog(("Passive_Skill_Vampire", 60f), ("Passive_Skill_Regeneration", 100f)));

            var item = (IEquipItem)service.CreateItem(ItemId, ["Passive_Skill_Vampire"], Rarity.Common, equipEffectChance: 0f, modifierMultiplier: 1f);

            Assert.AreEqual(1, item.Grants.Count, "A listed signature effect must land regardless of the chance.");
            Assert.AreEqual("Passive_Skill_Vampire", item.Grants[0].Id);
        }

        [TestMethod]
        public void GuaranteedChance_RollsOneGrantFromTheCatalog()
        {
            var service = CreateService(Catalog(("Passive_Skill_Regeneration", 100f)));

            var item = (IEquipItem)service.CreateItem(ItemId, [], Rarity.Common, equipEffectChance: 1f, modifierMultiplier: 1f);

            Assert.AreEqual(1, item.Grants.Count);
            Assert.AreEqual("Passive_Skill_Regeneration", item.Grants[0].Id);
        }

        [TestMethod]
        public void ZeroChance_GrantsNothing()
        {
            var service = CreateService(Catalog(("Passive_Skill_Regeneration", 100f)));

            var item = (IEquipItem)service.CreateItem(ItemId, [], Rarity.Common, equipEffectChance: 0f, modifierMultiplier: 1f);

            Assert.AreEqual(0, item.Grants.Count);
        }

        [TestMethod]
        public void UnknownSignatureId_IsReportedAndGrantsNothing()
        {
            var service = CreateService(Catalog(("Passive_Skill_Regeneration", 100f)));

            var item = (IEquipItem)service.CreateItem(ItemId, ["No_Such_Entry"], Rarity.Common, equipEffectChance: 1f, modifierMultiplier: 1f);

            Assert.AreEqual(0, item.Grants.Count, "An id missing from the catalog has no payload — refuse, never guess.");
        }

        [TestMethod]
        public void FixedRarities_AreNeverTouched()
        {
            var service = CreateService(Catalog(("Passive_Skill_Regeneration", 100f)), mintedRarity: Rarity.Mythic);

            var item = (IEquipItem)service.CreateItem(ItemId, ["Passive_Skill_Regeneration"], Rarity.Common, equipEffectChance: 1f, modifierMultiplier: 1f);

            Assert.AreEqual(0, item.Grants.Count, "Unique/Mythic templates are exclusives — generation must not add grants.");
            Assert.AreEqual(Rarity.Mythic, item.Rarity);
        }

        private static ItemCreationService CreateService(List<CraftingEffectOption> catalogEntries, Rarity mintedRarity = Rarity.Common)
        {
            var rnd = new DefaultRandomNumberGenerator(seed: 42);
            var minter = new Mock<IItemMinter>();
            minter.Setup(mock => mock.MintItem(ItemId))
                .Returns(() => new EquipItem(EquipmentPiece.Body, ItemId, []) { Rarity = mintedRarity });

            var dataProvider = new Mock<IItemDataProvider>();
            dataProvider.Setup(mock => mock.GetEquipItemBaseModifierPool(ItemId)).Returns([]);
            dataProvider.Setup(mock => mock.GetEquipItemModifierPool(ItemId)).Returns([]);

            var catalog = new Mock<ICraftingEffectProvider>();
            catalog.SetupGet(mock => mock.Effects).Returns(catalogEntries);

            return new ItemCreationService(dataProvider.Object, rnd, minter.Object, new ModifierMaterializer(rnd),
                catalog.Object, new GrantFactory(() => null, () => null, () => null));
        }

        private static List<CraftingEffectOption> Catalog(params (string Id, float Weight)[] entries) =>
            entries.Select(entry => new CraftingEffectOption(GrantKind.Passive, entry.Id, new Dictionary<string, float>()) { Weight = entry.Weight }).ToList();
    }
}
