namespace LastBreathTest.BattleSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Crafting.Source;
    using Moq;

    /// <summary>
    /// The live reroll pool model (owner decision 2026-07-16): NOTHING is stored on the item — every
    /// recraft recomputes family pool ∪ item pool ∪ used-resource descriptors from the data provider
    /// and scales the union by the item's PowerMultiplier. Loot (which stamps the drop's difficulty
    /// multiplier and has no used resources) is therefore rerollable exactly like crafted gear, and a
    /// json pool edit is visible on items that already exist.
    /// </summary>
    [TestClass]
    public class LiveRerollPoolTests
    {
        private const string LootItemId = "Amulet_Garnet";

        [TestMethod]
        public void LootItem_RerollsFromFamilyAndItemUnion_ScaledByPowerMultiplier()
        {
            // Distinct parameters mark the source: Accuracy lives ONLY in the item pool, Armor ONLY in
            // the family pool. A loot drop (no UsedResources) with PowerMultiplier 2 must draw from both,
            // and every rerolled line must carry 2x-scaled bounds.
            var provider = EmptyPoolProvider();
            provider.Setup(mock => mock.GetEquipItemModifierPool(LootItemId)).Returns(
            [
                new ParameterDescriptor(EntityParameter.Accuracy, ModifierValueType.Flat, new ValueRange(10f, 20f), ModifierScope.Global) { Weight = 10f, Affix = AffixKind.Prefix },
            ]);
            provider.Setup(mock => mock.GetEquipItemBaseModifierPool(LootItemId)).Returns(
            [
                new ParameterDescriptor(EntityParameter.Armor, ModifierValueType.Flat, new ValueRange(30f, 40f), ModifierScope.Global) { Weight = 10f, Affix = AffixKind.Prefix },
            ]);

            var seenParameters = new HashSet<EntityParameter>();
            for (int seed = 0; seed < 40; seed++)
            {
                var item = new EquipItem(EquipmentPiece.Amulet, LootItemId, []) { PowerMultiplier = 2f };
                var line = new SimpleModifier(EntityParameter.Health, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Prefix };
                item.AddAdditionalModifier(line);

                string? rerolledId = CreateUpgrader(seed, provider.Object).TryRecraftModifier(item, line.InstanceId);

                Assert.IsNotNull(rerolledId, $"seed {seed}: a loot drop must be rerollable from the live union");
                var rerolled = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId);
                seenParameters.Add(rerolled.EntityParameter);

                // The candidate scaled BEFORE the roll: the stamped range is the 2x source range.
                Assert.IsNotNull(rerolled.RolledRange, $"seed {seed}: a ranged candidate must stamp its provenance");
                (float expectedMin, float expectedMax) = rerolled.EntityParameter == EntityParameter.Accuracy ? (20f, 40f) : (60f, 80f);
                Assert.AreEqual(expectedMin, rerolled.RolledRange.Value.Min, 0.001f, $"seed {seed}");
                Assert.AreEqual(expectedMax, rerolled.RolledRange.Value.Max, 0.001f, $"seed {seed}");
                Assert.IsTrue(rerolled.BaseValue >= expectedMin && rerolled.BaseValue <= expectedMax,
                    $"seed {seed}: rolled {rerolled.BaseValue} outside the scaled bounds [{expectedMin}, {expectedMax}]");
            }

            // Across seeds the reroll must have drawn from BOTH pools — the union is alive.
            CollectionAssert.AreEquivalent(
                new[] { EntityParameter.Accuracy, EntityParameter.Armor }, seenParameters.ToArray());
        }

        [TestMethod]
        public void CraftedItem_UsedResourceDescriptorsJoinTheLivePool()
        {
            // The item's data pools hold only prefix bait — the ONLY suffix fodder comes from the
            // resource the item remembers being crafted from. Fixed values scale by the caliber too.
            const string resourceId = "Crafting_Resource_Iron";
            var provider = EmptyPoolProvider();
            provider.Setup(mock => mock.GetEquipItemModifierPool("Ring_Test")).Returns(
            [
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 10000f, Affix = AffixKind.Prefix },
            ]);
            provider.Setup(mock => mock.GetResourceDescriptors(resourceId)).Returns(
            [
                new ParameterDescriptor(EntityParameter.Strength, ModifierValueType.Flat, 10f, ModifierScope.Global) { Weight = 1f, Affix = AffixKind.Suffix },
            ]);

            var item = new EquipItem(EquipmentPiece.Ring, "Ring_Test", []) { PowerMultiplier = 1.5f };
            item.SaveUsedResources(new Dictionary<string, int> { [resourceId] = 2 }, new Dictionary<string, int>());
            var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix };
            item.AddAdditionalModifier(line);

            string? rerolledId = CreateUpgrader(seed: 42, provider.Object).TryRecraftModifier(item, line.InstanceId);

            Assert.IsNotNull(rerolledId);
            var rerolled = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId);
            Assert.AreEqual(EntityParameter.Strength, rerolled.EntityParameter);
            Assert.AreEqual(AffixKind.Suffix, rerolled.Affix);
            Assert.AreEqual(10f * 1.5f, rerolled.BaseValue, 0.001f); // resource line scaled by the item's caliber
        }

        [TestMethod]
        public void CraftedItem_OptionalResourceDescriptorsJoinTheLivePoolToo()
        {
            // The design pool is required + OPTIONAL used resources: here the only suffix fodder sits
            // in the item's remembered additive (optional) part.
            const string essenceId = "Crafting_Resource_Essence_Barrier";
            var provider = EmptyPoolProvider();
            provider.Setup(mock => mock.GetEquipItemModifierPool("Ring_Test")).Returns(
            [
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 10000f, Affix = AffixKind.Prefix },
            ]);
            provider.Setup(mock => mock.GetResourceDescriptors(essenceId)).Returns(
            [
                new ParameterDescriptor(EntityParameter.Barrier, ModifierValueType.Flat, 20f, ModifierScope.Global) { Weight = 1f, Affix = AffixKind.Suffix },
            ]);

            var item = new EquipItem(EquipmentPiece.Ring, "Ring_Test", []);
            item.SaveUsedResources(new Dictionary<string, int>(), new Dictionary<string, int> { [essenceId] = 1 });
            var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix };
            item.AddAdditionalModifier(line);

            string? rerolledId = CreateUpgrader(seed: 42, provider.Object).TryRecraftModifier(item, line.InstanceId);

            Assert.IsNotNull(rerolledId);
            var rerolled = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId);
            Assert.AreEqual(EntityParameter.Barrier, rerolled.EntityParameter);
            Assert.AreEqual(AffixKind.Suffix, rerolled.Affix);
        }

        [TestMethod]
        public void PoolEditInData_IsVisibleOnExistingItems()
        {
            // The whole point of the live model: the provider's answer changes (json edit / hot reload)
            // and an item created long ago rerolls from the NEW pool without any migration.
            var familyPool = new List<IModifierDescriptor>
            {
                new ParameterDescriptor(EntityParameter.Accuracy, ModifierValueType.Flat, 30f, ModifierScope.Global) { Weight = 10f, Affix = AffixKind.Prefix },
            };
            var provider = EmptyPoolProvider();
            provider.Setup(mock => mock.GetEquipItemBaseModifierPool(LootItemId)).Returns(() => [.. familyPool]);

            var item = new EquipItem(EquipmentPiece.Amulet, LootItemId, []);
            var line = new SimpleModifier(EntityParameter.Health, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Prefix };
            item.AddAdditionalModifier(line);

            string? firstId = CreateUpgrader(seed: 1, provider.Object).TryRecraftModifier(item, line.InstanceId);
            Assert.IsNotNull(firstId);
            Assert.AreEqual(EntityParameter.Accuracy, item.Modifiers.Single(modifier => modifier.InstanceId == firstId).EntityParameter);

            // "Edit the json": the family pool now holds a different line.
            familyPool.Clear();
            familyPool.Add(new ParameterDescriptor(EntityParameter.CriticalChance, ModifierValueType.Flat, 0.1f, ModifierScope.Global) { Weight = 10f, Affix = AffixKind.Prefix });

            string? secondId = CreateUpgrader(seed: 2, provider.Object).TryRecraftModifier(item, firstId!);

            Assert.IsNotNull(secondId);
            Assert.AreEqual(EntityParameter.CriticalChance, item.Modifiers.Single(modifier => modifier.InstanceId == secondId).EntityParameter);
        }

        [TestMethod]
        public void ResourceEntriesOfAnotherEquipmentCategory_NeverEnterTheRerollPool()
        {
            // Stage 2 rework: the amulet was crafted from an ore whose descriptors split by equipment
            // category — the Weapon-only damage entry (huge weight on purpose) must be gated out of a
            // Jewellery reroll, while the unrestricted mana entry from the same ore stays reachable.
            var provider = EmptyPoolProvider();
            provider.Setup(mock => mock.GetResourceDescriptors("Ore")).Returns(
            [
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, new ValueRange(10f, 20f), ModifierScope.Global) { Weight = 1000f, Affix = AffixKind.Prefix, OnlyFor = EquipmentCategory.Weapon },
                new ParameterDescriptor(EntityParameter.Mana, ModifierValueType.Flat, new ValueRange(10f, 20f), ModifierScope.Global) { Weight = 10f, Affix = AffixKind.Prefix },
            ]);

            var seenParameters = new HashSet<EntityParameter>();
            for (int seed = 0; seed < 40; seed++)
            {
                var item = new EquipItem(EquipmentPiece.Amulet, LootItemId, []);
                item.SaveUsedResources(new Dictionary<string, int> { ["Ore"] = 1 }, new Dictionary<string, int>());
                var line = new SimpleModifier(EntityParameter.Health, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Prefix };
                item.AddAdditionalModifier(line);

                string? rerolledId = CreateUpgrader(seed, provider.Object).TryRecraftModifier(item, line.InstanceId);

                Assert.IsNotNull(rerolledId, $"seed {seed}");
                seenParameters.Add(((SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId)).EntityParameter);
            }

            Assert.IsTrue(seenParameters.Contains(EntityParameter.Mana), "the unrestricted entry of the used resource must stay reachable");
            Assert.IsFalse(seenParameters.Contains(EntityParameter.PhysicalDamage), "a Weapon-only entry must never land on a Jewellery item");
        }

        [TestMethod]
        public void EssenceInTheOptionalSlot_FeedsTheRerollPool_CategoryGated()
        {
            // Stage-4 UI fix companion: a plain essence passed as the operation's additive joins
            // the reroll union with its own descriptors — while its Weapon-only entry still never
            // lands on a Jewellery item.
            var provider = EmptyPoolProvider();
            provider.Setup(mock => mock.GetResourceDescriptors("Essence")).Returns(
            [
                new ParameterDescriptor(EntityParameter.Barrier, ModifierValueType.Flat, new ValueRange(10f, 20f), ModifierScope.Global) { Weight = 10f, Affix = AffixKind.Prefix },
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, new ValueRange(10f, 20f), ModifierScope.Global) { Weight = 1000f, Affix = AffixKind.Prefix, OnlyFor = EquipmentCategory.Weapon },
            ]);

            var seenParameters = new HashSet<EntityParameter>();
            for (int seed = 0; seed < 30; seed++)
            {
                var item = new EquipItem(EquipmentPiece.Amulet, LootItemId, []);
                var line = new SimpleModifier(EntityParameter.Health, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Prefix };
                item.AddAdditionalModifier(line);

                string? rerolledId = CreateUpgrader(seed, provider.Object).TryRecraftModifier(item, line.InstanceId, ["Essence"]);
                Assert.IsNotNull(rerolledId, $"seed {seed}: the essence descriptors must make the reroll possible");
                seenParameters.Add(((SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId)).EntityParameter);
            }

            Assert.IsTrue(seenParameters.Contains(EntityParameter.Barrier), "the essence's entry must be rollable");
            Assert.IsFalse(seenParameters.Contains(EntityParameter.PhysicalDamage), "a Weapon-only essence entry must never land on a Jewellery item");
        }

        private static ItemUpgrader CreateUpgrader(int seed, IItemDataProvider provider)
        {
            var rnd = new DefaultRandomNumberGenerator(seed);
            return new ItemUpgrader(rnd, Mock.Of<ICraftingMastery>(), provider,
                Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(rnd));
        }

        /// <summary>Loose mocks return null for IReadOnlyList getters — the live pool assembly needs
        /// honest empty pools (the production contract) for every unstubbed id.</summary>
        private static Mock<IItemDataProvider> EmptyPoolProvider()
        {
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetEquipItemBaseModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetResourceDescriptors(It.IsAny<string>())).Returns([]);
            return provider;
        }
    }
}
