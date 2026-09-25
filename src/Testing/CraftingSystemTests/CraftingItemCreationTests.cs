namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Moq;

    /// <summary>The crafting roll end-to-end: mastery rolls the rarity (slot split), mastery quality
    /// scales the resource descriptors, the affix roller fills the slots, and the scaled+flattened
    /// pool is saved on the item as affixed reroll fodder.</summary>
    [TestClass]
    public class CraftingItemCreationTests
    {
        private const string BlueprintJson = """
        {
            "items": [
                {
                    "id": "Test_Chest",
                    "equipmentPart": "Body",
                    "rarity": "Common",
                    "updateLevel": 0,
                    "maxUpdateLevel": 12,
                    "tags": [],
                    "implicits": [],
                    "modifiers": []
                }
            ]
        }
        """;

        [TestMethod]
        public void CreateItemByRecipe_SlotsFollowTheRolledRarity()
        {
            var pool = RichResourcePool();
            foreach ((var rarity, int expectedPrefixes, int expectedSuffixes) in
                     (ReadOnlySpan<(Rarity, int, int)>)[(Rarity.Rare, 1, 1), (Rarity.Legendary, 2, 2)])
            {
                var item = (IEquipItem)CreateService(rarity, qualityMultiplier: 1f, seed: 42)
                    .CreateItemByRecipe("Recipe_Test", pool);

                Assert.AreEqual(rarity, item.Rarity);
                var lines = item.Modifiers.OfType<SimpleModifier>().ToList();
                Assert.AreEqual(expectedPrefixes, lines.Count(line => line.Affix == AffixKind.Prefix));
                Assert.AreEqual(expectedSuffixes, lines.Count(line => line.Affix == AffixKind.Suffix));
            }
        }

        [TestMethod]
        public void CreateItemByRecipe_UncommonRollsExactlyOneLine()
        {
            var item = (IEquipItem)CreateService(Rarity.Uncommon, qualityMultiplier: 1f, seed: 7)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool());

            var lines = item.Modifiers.OfType<SimpleModifier>().Where(line => line.Affix != AffixKind.None).ToList();
            Assert.AreEqual(1, lines.Count);
        }

        [TestMethod]
        public void CreateItemByRecipe_StampsQualityAsPowerMultiplierAndScalesRolls()
        {
            var item = (IEquipItem)CreateService(Rarity.Rare, qualityMultiplier: 2f, seed: 42)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool());

            // The quality is the item's caliber from now on: the live reroll pool rescales by it.
            Assert.AreEqual(2f, item.PowerMultiplier, 0.001f);
            // Quality scaling doubles the rolled values — every line's magnitude is 2x its source entry.
            foreach (var line in item.Modifiers.OfType<SimpleModifier>().Where(line => line.Affix != AffixKind.None))
            {
                float sourceValue = RichResourcePool().OfType<ParameterDescriptor>()
                    .Single(descriptor => descriptor.Parameter == line.EntityParameter).Value.Min;
                Assert.AreEqual(sourceValue * 2f, line.BaseValue, 0.001f);
            }
        }

        [TestMethod]
        public void CreateItemByRecipe_RollsFromFamilyItemAndResourceUnion()
        {
            // Resources offer ONLY prefixes; the result item's family and own pools offer ONLY suffixes.
            // A Rare roll (1 prefix + 1 suffix) can fill its suffix slot from the data pools alone —
            // proof the creation roll unions all three sources, not just the used resources.
            List<IModifierDescriptor> resourcePool = [Entry(EntityParameter.PhysicalDamage, 100f, AffixKind.Prefix)];
            var familyEntry = Entry(EntityParameter.Strength, 10f, AffixKind.Suffix);
            var itemPoolEntry = Entry(EntityParameter.Accuracy, 30f, AffixKind.Suffix);

            var suffixes = new HashSet<EntityParameter>();
            for (int seed = 0; seed < 30; seed++)
            {
                var item = (IEquipItem)CreateService(Rarity.Rare, qualityMultiplier: 1f, seed,
                        familyPool: [familyEntry], itemPool: [itemPoolEntry])
                    .CreateItemByRecipe("Recipe_Test", resourcePool);

                var lines = item.Modifiers.OfType<SimpleModifier>().ToList();
                Assert.AreEqual(EntityParameter.PhysicalDamage, lines.Single(line => line.Affix == AffixKind.Prefix).EntityParameter);
                suffixes.Add(lines.Single(line => line.Affix == AffixKind.Suffix).EntityParameter);
            }

            // Across seeds the suffix slot must have drawn from BOTH data pools.
            CollectionAssert.AreEquivalent(
                new[] { EntityParameter.Strength, EntityParameter.Accuracy }, suffixes.ToArray());
        }

        [TestMethod]
        public void CreateItemByRecipe_RarityFloor_RaisesAWorseRoll()
        {
            // Mastery rolls Uncommon; a Journeyman-grade floor (Rare) must lift the result to Rare —
            // BEFORE the slot split (a Rare item fills 1+1 slots, an Uncommon only one).
            var item = (IEquipItem)CreateService(Rarity.Uncommon, qualityMultiplier: 1f, seed: 42)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool(), minRarity: Rarity.Rare);

            Assert.AreEqual(Rarity.Rare, item.Rarity);
            var lines = item.Modifiers.OfType<SimpleModifier>().Where(line => line.Affix != AffixKind.None).ToList();
            Assert.AreEqual(1, lines.Count(line => line.Affix == AffixKind.Prefix), "The raised rarity must buy its full slot split.");
            Assert.AreEqual(1, lines.Count(line => line.Affix == AffixKind.Suffix), "The raised rarity must buy its full slot split.");
        }

        [TestMethod]
        public void CreateItemByRecipe_RarityFloor_NeverLowersABetterRoll()
        {
            var item = (IEquipItem)CreateService(Rarity.Legendary, qualityMultiplier: 1f, seed: 42)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool(), minRarity: Rarity.Rare);

            Assert.AreEqual(Rarity.Legendary, item.Rarity, "A floor is a guarantee, not a cap.");
        }

        [TestMethod]
        public void CreateItemByRecipe_ExtraEffectRoll_GrantsFromTheCatalog()
        {
            var catalog = new List<CraftingEffectOption>
            {
                new(GrantKind.Passive, "Passive_Skill_Regeneration", new Dictionary<string, float> { ["percent"] = 0.03f }) { Weight = 100f },
            };
            var item = (IEquipItem)CreateService(Rarity.Rare, qualityMultiplier: 1f, seed: 42,
                    extraEffectChance: 1f, effectCatalog: catalog)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool());

            Assert.AreEqual(1, item.Grants.Count, "A guaranteed roll must land exactly one grant.");
            Assert.AreEqual("Passive_Skill_Regeneration", item.Grants[0].Id);
        }

        [TestMethod]
        public void CreateItemByRecipe_ExtraEffectRoll_ZeroChanceGrantsNothing()
        {
            var catalog = new List<CraftingEffectOption>
            {
                new(GrantKind.Passive, "Passive_Skill_Regeneration", new Dictionary<string, float> { ["percent"] = 0.03f }) { Weight = 100f },
            };
            var item = (IEquipItem)CreateService(Rarity.Rare, qualityMultiplier: 1f, seed: 42,
                    extraEffectChance: 0f, effectCatalog: catalog)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool());

            Assert.AreEqual(0, item.Grants.Count);
        }

        [TestMethod]
        public void CreateItemByRecipe_EmptyEffectCatalog_ConsumesNoRoll()
        {
            // Same seed, catalog present-with-zero-chance vs absent: the affix lines must be identical —
            // an empty catalog short-circuits BEFORE the RNG, so legacy seeded sequences never shift.
            var withEmpty = (IEquipItem)CreateService(Rarity.Rare, 1f, seed: 42)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool());
            var reference = (IEquipItem)CreateService(Rarity.Rare, 1f, seed: 42)
                .CreateItemByRecipe("Recipe_Test", RichResourcePool());

            CollectionAssert.AreEqual(
                reference.Modifiers.Select(line => (line.EntityParameter, line.Value)).ToList(),
                withEmpty.Modifiers.Select(line => (line.EntityParameter, line.Value)).ToList());
        }

        private static List<IModifierDescriptor> RichResourcePool() =>
        [
            Entry(EntityParameter.PhysicalDamage, 100f, AffixKind.Prefix),
            Entry(EntityParameter.Health, 50f, AffixKind.Prefix),
            Entry(EntityParameter.Armor, 40f, AffixKind.Prefix),
            Entry(EntityParameter.Strength, 10f, AffixKind.Suffix),
            Entry(EntityParameter.Dexterity, 10f, AffixKind.Suffix),
            Entry(EntityParameter.Accuracy, 30f, AffixKind.Suffix),
        ];

        private static ParameterDescriptor Entry(EntityParameter parameter, float value, AffixKind affix) =>
            new(parameter, ModifierValueType.Flat, value, ModifierScope.Global) { Weight = 10f, Affix = affix };

        private static Crafting.Services.ItemCreationService CreateService(Rarity rolledRarity, float qualityMultiplier, int seed,
            List<IModifierDescriptor>? familyPool = null, List<IModifierDescriptor>? itemPool = null,
            float extraEffectChance = 0f, List<CraftingEffectOption>? effectCatalog = null)
        {
            var mastery = new Mock<ICraftingMastery>();
            mastery.Setup(mock => mock.RollRarity(It.IsAny<float>())).Returns(rolledRarity);
            mastery.Setup(mock => mock.GetCurrentValueMultiplier(It.IsAny<float>())).Returns(qualityMultiplier);
            mastery.Setup(mock => mock.GetExtraEffectChance()).Returns(extraEffectChance);

            var recipe = new Mock<ICraftingRecipe>();
            recipe.SetupGet(mock => mock.ItemType).Returns(ItemType.Equipment);
            recipe.SetupGet(mock => mock.ResultItemId).Returns("Test_Chest");
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetRecipe("Recipe_Test")).Returns(recipe.Object);
            provider.Setup(mock => mock.GetEquipItemBaseModifierPool("Test_Chest")).Returns(familyPool ?? []);
            provider.Setup(mock => mock.GetEquipItemModifierPool("Test_Chest")).Returns(itemPool ?? []);

            var rnd = new DefaultRandomNumberGenerator(seed);
            var materializer = new ModifierMaterializer(rnd);
            var blueprints = new BlueprintProvider(
                new DataParser(new LootGeneration.Internal.ItemGameDataFactory()).ParseEquipItems(BlueprintJson));
            var minter = new EquipItemMinter(blueprints, new LootGeneration.Internal.ItemGameDataFactory(),
                new GrantFactory(() => null, () => null, () => null), materializer, rnd);

            var effects = new Mock<ICraftingEffectProvider>();
            effects.SetupGet(mock => mock.Effects).Returns(effectCatalog ?? []);

            return new Crafting.Services.ItemCreationService(mastery.Object, rnd, provider.Object, materializer, minter,
                effects.Object, new GrantFactory(() => null, () => null, () => null));
        }

        private sealed class BlueprintProvider(IEnumerable<EquipItemBlueprint> blueprints) : IEquipBlueprintProvider
        {
            private readonly Dictionary<string, EquipItemBlueprint> _map = blueprints.ToDictionary(blueprint => blueprint.Id);

            public EquipItemBlueprint? GetBlueprint(string id) => _map.GetValueOrDefault(id);
            public IEnumerable<EquipItemBlueprint> AllBlueprints => _map.Values;
        }
    }
}
