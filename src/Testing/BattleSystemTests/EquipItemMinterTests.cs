namespace LastBreathTest.BattleSystemTests
{
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Moq;

    /// <summary>The minting seam: blueprint -> rolled item. Seeded RNG, LootGeneration factory
    /// (plain domain objects, no Godot native — Icon is lazy and never touched here).</summary>
    [TestClass]
    public class EquipItemMinterTests
    {
        private const string FixedItemJson = """
        {
            "items": [
                {
                    "id": "Test_Chest",
                    "equipmentPart": "Body",
                    "rarity": "Rare",
                    "updateLevel": 3,
                    "maxUpdateLevel": 12,
                    "tags": [],
                    "implicits": [
                        { "parameter": "Armor", "modifierType": "flat", "value": 50 }
                    ],
                    "modifiers": [
                        { "parameter": "Health", "modifierType": "flat", "value": 100 }
                    ]
                }
            ]
        }
        """;

        private const string RangedValueJson = """
        {
            "items": [
                {
                    "id": "Test_Ranged",
                    "equipmentPart": "Body",
                    "rarity": "Rare",
                    "updateLevel": 0,
                    "maxUpdateLevel": 12,
                    "tags": [],
                    "implicits": [],
                    "modifiers": [
                        { "parameter": "Health", "modifierType": "flat", "value": { "min": 10, "max": 20 } }
                    ]
                }
            ]
        }
        """;

        private const string MythicJson = """
        {
            "items": [
                {
                    "id": "Test_Mythic",
                    "equipmentPart": "Weapon",
                    "weaponType": "Sword",
                    "handedness": "OneHanded",
                    "damage": 220,
                    "criticalChance": 0.06,
                    "criticalDamage": 1.6,
                    "rarity": "Mythic",
                    "updateLevel": { "min": 1, "max": 69 },
                    "maxUpdateLevel": 69,
                    "tags": [],
                    "implicits": [],
                    "modifiers": [
                        { "parameter": "Health", "modifierType": "flat", "value": 100 }
                    ]
                }
            ]
        }
        """;

        [TestMethod]
        public void Mint_FixedContent_MatchesLegacyTemplateScaling()
        {
            // Bit-for-bit with the pre-blueprint template parse: level 3 -> multiplier 1.3 on every line.
            var item = CreateMinter(FixedItemJson, seed: 42).Mint("Test_Chest");

            Assert.AreEqual(Rarity.Rare, item.Rarity);
            Assert.AreEqual(3, item.UpdateLevel);
            Assert.AreEqual(12, item.MaxUpdateLevel);
            Assert.AreEqual(50f, item.Implicits[0].BaseValue, 0.001f);
            Assert.AreEqual(50f * 1.15f, item.Implicits[0].Value, 0.001f);
            var health = item.Modifiers.Single();
            Assert.AreEqual(100f, health.BaseValue, 0.001f);
            Assert.AreEqual(100f * 1.15f, health.Value, 0.001f);
        }

        [TestMethod]
        public void Mint_FixedContent_IsDeterministicAcrossSeeds()
        {
            // Fixed values never touch the RNG: any two seeds mint identical items.
            var first = CreateMinter(FixedItemJson, seed: 1).Mint("Test_Chest");
            var second = CreateMinter(FixedItemJson, seed: 999).Mint("Test_Chest");

            Assert.AreEqual(first.Modifiers.Single().Value, second.Modifiers.Single().Value, 0.0001f);
            Assert.AreEqual(first.Implicits[0].Value, second.Implicits[0].Value, 0.0001f);
        }

        [TestMethod]
        public void Mint_NonMythicWithStartingLevel_SharpensFurther()
        {
            var item = CreateMinter(FixedItemJson, seed: 42).Mint("Test_Chest");

            Assert.IsTrue(item.Upgrade(), "A non-mythic minted at its starting level must accept further sharpening.");
            Assert.AreEqual(4, item.UpdateLevel);
        }

        [TestMethod]
        public void Mint_RangedValue_RollsWithinBounds()
        {
            var item = CreateMinter(RangedValueJson, seed: 7).Mint("Test_Ranged");

            var line = (SimpleModifier)item.Modifiers.Single();
            Assert.IsTrue(line.BaseValue is >= 10f and <= 20f, $"Rolled {line.BaseValue} escapes [10..20].");
            Assert.IsNotNull(line.RolledRange, "A ranged line must be stamped with its source spread.");
            Assert.AreEqual(10f, line.RolledRange.Value.Min, 0.001f);
            Assert.AreEqual(20f, line.RolledRange.Value.Max, 0.001f);
        }

        [TestMethod]
        public void Mint_RangedValue_DifferentSeedsProduceDifferentRolls()
        {
            float first = CreateMinter(RangedValueJson, seed: 1).Mint("Test_Ranged").Modifiers.Single().BaseValue;
            float second = CreateMinter(RangedValueJson, seed: 2).Mint("Test_Ranged").Modifiers.Single().BaseValue;

            Assert.AreNotEqual(first, second, 0.0001f, "Two seeds rolled the same value — the range looks dead.");
        }

        [TestMethod]
        public void Mint_MythicWithRangedLevel_LevelIsItsWholeProgression()
        {
            var item = CreateMinter(MythicJson, seed: 20260716).Mint("Test_Mythic");

            Assert.AreEqual(Rarity.Mythic, item.Rarity);
            Assert.IsFalse(item.IsSealed, "Data-born mythics are NOT sealed (only ascension seals).");
            Assert.AreEqual(item.MaxUpdateLevel, item.UpdateLevel, "Rolled level IS the mythic's cap.");
            Assert.IsTrue(item.UpdateLevel is >= 1 and <= 69, $"Rolled level {item.UpdateLevel} escapes [1..69].");
            var line = item.Modifiers.Single();
            Assert.AreEqual(100f * (1f + item.UpdateLevel * 0.05f), line.Value, 0.01f);
            Assert.IsFalse(item.Upgrade(), "A mythic minted at its rolled cap must not sharpen further.");
        }

        [TestMethod]
        public void Mint_Weapon_CarriesTheCombatBlock()
        {
            var item = CreateMinter(MythicJson, seed: 3).Mint("Test_Mythic");

            var weapon = (IWeaponItem)item;
            Assert.AreEqual(WeaponType.Sword, weapon.WeaponType);
            Assert.AreEqual(Handedness.OneHanded, weapon.Handedness);
            Assert.AreEqual(0.06f, weapon.CriticalChance, 0.001f);
            Assert.AreEqual(1.6f, weapon.CriticalDamage, 0.001f);
        }

        [TestMethod]
        public void Mint_TwoMintsOfOneId_AreIndependentInstances()
        {
            var minter = CreateMinter(FixedItemJson, seed: 42);

            var first = minter.Mint("Test_Chest");
            var second = minter.Mint("Test_Chest");

            Assert.AreNotEqual(first.InstanceId, second.InstanceId);
            first.Upgrade();
            Assert.AreEqual(3, second.UpdateLevel, "Mints must not share state.");
        }

        [TestMethod]
        public void MintItem_EquipId_MintsAFreshRoll()
        {
            var blueprints = ParseBlueprints(FixedItemJson);
            var items = new Mock<IItemDataProvider>(MockBehavior.Strict); // strict: an equip id must never reach CopyItem
            var facade = new ItemMinter(blueprints, CreateMinter(blueprints, seed: 42), items.Object);

            var item = facade.MintItem("Test_Chest");

            Assert.IsTrue(item is IEquipItem, "An equip id must mint an equip item.");
            Assert.AreEqual(3, ((IEquipItem)item).UpdateLevel);
        }

        [TestMethod]
        public void MintItem_PlainResourceId_FallsBackToCopy()
        {
            var blueprints = ParseBlueprints(FixedItemJson);
            var copy = Mock.Of<IItem>(resource => resource.Id == "Coal");
            var items = new Mock<IItemDataProvider>();
            items.Setup(provider => provider.CopyItem("Coal")).Returns(copy);
            var facade = new ItemMinter(blueprints, CreateMinter(blueprints, seed: 42), items.Object);

            Assert.AreSame(copy, facade.MintItem("Coal"));
            items.Verify(provider => provider.CopyItem("Coal"), Times.Once);
        }

        private static BlueprintProvider ParseBlueprints(string json) =>
            new(new DataParser(new LootGeneration.Internal.ItemGameDataFactory()).ParseEquipItems(json));

        private static IEquipItemMinter CreateMinter(string json, int seed) => CreateMinter(ParseBlueprints(json), seed);

        private static IEquipItemMinter CreateMinter(BlueprintProvider blueprints, int seed)
        {
            var rnd = new DefaultRandomNumberGenerator(seed);
            return new EquipItemMinter(blueprints, new LootGeneration.Internal.ItemGameDataFactory(), new ModifierMaterializer(rnd), rnd);
        }

        private sealed class BlueprintProvider(IEnumerable<EquipItemBlueprint> blueprints) : IEquipBlueprintProvider
        {
            private readonly Dictionary<string, EquipItemBlueprint> _map = blueprints.ToDictionary(blueprint => blueprint.Id);

            public EquipItemBlueprint? GetBlueprint(string id) => _map.GetValueOrDefault(id);
            public IEnumerable<EquipItemBlueprint> AllBlueprints => _map.Values;
        }
    }
}
