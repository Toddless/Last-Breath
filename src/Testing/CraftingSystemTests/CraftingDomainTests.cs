namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Modifiers;
    using Core.Results;
    using Crafting.Source;
    using Moq;

    /// <summary>
    /// The regressions stage A fixed must stay fixed: all-or-nothing spending, the max-level
    /// short-circuit before any cost, and recraft refusing instead of looping forever. Upgrader paths
    /// under test deliberately end BEFORE any roll (a null RNG proves it). Ascension tests DO roll:
    /// the whole crafting stack now draws from IRandomNumberGenerator, mocked or seeded here.
    /// </summary>
    [TestClass]
    public class CraftingDomainTests
    {
        private Mock<IInventory> _inventory = null!;
        private CraftingResources _resources = null!;

        [TestInitialize]
        public void Setup()
        {
            _inventory = new Mock<IInventory>();
            _resources = new CraftingResources(_inventory.Object);
        }

        [TestMethod]
        public void TrySpend_MissingOneResource_SpendsNothing()
        {
            _inventory.Setup(mock => mock.GetTotalItemAmount("Ore")).Returns(10);
            _inventory.Setup(mock => mock.GetTotalItemAmount("Wood")).Returns(1);

            bool spent = _resources.TrySpend(new Dictionary<string, int> { ["Ore"] = 5, ["Wood"] = 2 });

            Assert.IsFalse(spent);
            _inventory.Verify(mock => mock.RemoveItemById(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void TrySpend_EverythingPresent_RemovesExactAmounts()
        {
            _inventory.Setup(mock => mock.GetTotalItemAmount(It.IsAny<string>())).Returns(10);

            bool spent = _resources.TrySpend(new Dictionary<string, int> { ["Ore"] = 5, ["Wood"] = 2 });

            Assert.IsTrue(spent);
            _inventory.Verify(mock => mock.RemoveItemById("Ore", 5), Times.Once);
            _inventory.Verify(mock => mock.RemoveItemById("Wood", 2), Times.Once);
        }

        [TestMethod]
        public void TryUpgradeItem_AtMaxLevel_ShortCircuitsBeforeAnyRoll()
        {
            var upgrader = CreateUpgrader();
            var item = new Mock<IEquipItem>();
            item.SetupGet(mock => mock.UpdateLevel).Returns(12);
            item.SetupGet(mock => mock.MaxUpdateLevel).Returns(12);

            // A null RNG proves no roll happens: reaching one would throw.
            var result = upgrader.TryUpgradeItem(item.Object);

            Assert.AreEqual(ItemUpgradeResult.ReachedMaxLevel, result);
            item.Verify(mock => mock.Upgrade(It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void TryRecraftModifier_InstanceIdNotOnItem_RefusesWithoutRolling()
        {
            var upgrader = CreateUpgrader();
            var item = new Mock<IEquipItem>();
            item.SetupGet(mock => mock.Modifiers).Returns([]);
            item.SetupGet(mock => mock.ContextModifiers).Returns([]);

            var result = upgrader.TryRecraftModifier(item.Object, modifierInstanceId: "missing");

            Assert.IsNull(result);
            item.Verify(mock => mock.RemoveAdditionalModifier(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TryRecraftModifier_ExhaustedPool_RefusesInsteadOfLooping()
        {
            var upgrader = CreateUpgrader();
            var onItem = new Mock<IModifierInstance>();
            onItem.SetupGet(mock => mock.InstanceId).Returns("mod-1");
            var item = new Mock<IEquipItem>();
            item.SetupGet(mock => mock.Id).Returns("Mock_Item");
            item.SetupGet(mock => mock.Modifiers).Returns([onItem.Object]);
            item.SetupGet(mock => mock.ContextModifiers).Returns([]);
            item.SetupGet(mock => mock.UsedRequiredResources).Returns(new Dictionary<string, int>());
            item.SetupGet(mock => mock.UsedOptionalResources).Returns(new Dictionary<string, int>());

            // The line is on the item, but the live pool (data provider knows nothing about this id,
            // no used resources) is empty — nothing to draw, so refuse instead of looping.
            var result = upgrader.TryRecraftModifier(item.Object, "mod-1");

            Assert.IsNull(result);
            item.Verify(mock => mock.RemoveAdditionalModifier(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TryAscendItem_GiftLandsOnItemAndItemIsSealed()
        {
            var item = CreateMaxedLegendary();
            int linesBefore = item.Modifiers.Count;
            var ascender = CreateAscender(
            [
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Increase, 0.4f, ModifierScope.Global) { Weight = 100f, Affix = AffixKind.Prefix },
            ]);

            var result = ascender.TryAscendItem(item);

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(item.IsSealed);
            Assert.AreEqual(Rarity.Mythic, item.Rarity);
            Assert.AreEqual(1, result.GiftedModifierIds.Count);
            Assert.AreEqual(linesBefore + 1, item.Modifiers.Count);
            // The reported id must point at a line that ACTUALLY sits on the item — the old order
            // rolled the gift after the seal, so Add* silently dropped it.
            var gift = item.Modifiers.FirstOrDefault(modifier => modifier.InstanceId == result.GiftedModifierIds[0]);
            Assert.IsNotNull(gift);
            Assert.AreEqual(EntityParameter.PhysicalDamage, gift.EntityParameter);
            Assert.AreEqual(0.4f, gift.BaseValue, 0.001f);
        }

        [TestMethod]
        public void TryAscendItem_CompositePoolEntry_LandsAsAtomicInstances()
        {
            var item = CreateMaxedLegendary();
            int linesBefore = item.Modifiers.Count;
            var composite = new CompositeDescriptor(
            [
                new ParameterDescriptor(EntityParameter.Armor, ModifierValueType.Multiplicative, 0.25f, ModifierScope.Global),
                new ParameterDescriptor(EntityParameter.Evade, ModifierValueType.Multiplicative, 0.25f, ModifierScope.Global),
            ]) { Weight = 30f, Affix = AffixKind.Prefix };
            var ascender = CreateAscender([composite]);

            var result = ascender.TryAscendItem(item);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(2, result.GiftedModifierIds.Count);
            Assert.AreEqual(linesBefore + 2, item.Modifiers.Count);
            foreach (string giftId in result.GiftedModifierIds)
            {
                var line = item.Modifiers.FirstOrDefault(modifier => modifier.InstanceId == giftId);
                Assert.IsNotNull(line);
                Assert.IsNotInstanceOfType<CompositeModifier>(line); // parts land atomically, not as the pool entry
                Assert.AreEqual(0.25f, line.BaseValue, 0.001f);
                Assert.AreEqual(ModifierScope.Global, line.Scope);
            }

            CollectionAssert.AreEquivalent(
                new[] { EntityParameter.Armor, EntityParameter.Evade },
                result.GiftedModifierIds.Select(giftId => item.Modifiers.First(modifier => modifier.InstanceId == giftId).EntityParameter).ToArray());
        }

        [TestMethod]
        public void TryRecraftModifier_SuffixLine_RerollsOnlyIntoSuffix()
        {
            // The prefix candidate carries an overwhelming weight: if the affix filter broke,
            // it would win virtually every seed.
            var prefixBait = new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 10000f, Affix = AffixKind.Prefix };
            var suffixHome = new ParameterDescriptor(EntityParameter.Strength, ModifierValueType.Flat, 10f, ModifierScope.Global) { Weight = 1f, Affix = AffixKind.Suffix };
            for (int seed = 0; seed < 20; seed++)
            {
                var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
                var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix };
                item.AddAdditionalModifier(line);

                string? rerolledId = CreateRecraftUpgrader(seed, PoolProvider("Band", prefixBait, suffixHome)).TryRecraftModifier(item, line.InstanceId);

                Assert.IsNotNull(rerolledId);
                var rerolled = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId);
                Assert.AreEqual(AffixKind.Suffix, rerolled.Affix, $"seed {seed} broke the slot family");
                Assert.AreEqual(EntityParameter.Strength, rerolled.EntityParameter);
            }
        }

        [TestMethod]
        public void TryRecraftModifier_SuffixContextLine_RerollsOnlyIntoSuffix()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var entry = new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.2f) { Affix = AffixKind.Suffix };
            item.AddAdditionalContextModifier(entry);
            var provider = PoolProvider("Band",
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 10000f, Affix = AffixKind.Prefix },
                new ContextDescriptor(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.35f) { Weight = 1f, Affix = AffixKind.Suffix });

            string? rerolledId = CreateRecraftUpgrader(seed: 42, provider).TryRecraftModifier(item, entry.InstanceId);

            Assert.IsNotNull(rerolledId);
            Assert.AreEqual(0, item.Modifiers.Count, "The reroll leaked into the entity channel.");
            var rerolled = item.ContextModifiers.Single(context => context.InstanceId == rerolledId);
            Assert.AreEqual(AffixKind.Suffix, rerolled.Affix);
            Assert.AreEqual(0.35f, rerolled.BaseValue, 0.001f);
        }

        [TestMethod]
        public void TryRecraftModifier_PoolLacksTargetKind_RefusesAndKeepsTheLine()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix };
            item.AddAdditionalModifier(line);
            var provider = PoolProvider("Band",
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 100f, Affix = AffixKind.Prefix });

            string? rerolledId = CreateRecraftUpgrader(seed: 42, provider).TryRecraftModifier(item, line.InstanceId);

            // No suffix fodder anywhere — refuse; the handler spends nothing on a null result.
            Assert.IsNull(rerolledId);
            Assert.AreEqual(line.InstanceId, item.Modifiers.Single().InstanceId, "A refused reroll must not touch the line.");
        }

        [TestMethod]
        public void TryRecraftModifier_NoneLine_RerollsFromFullPool()
        {
            // Legacy tolerance (documented decision): a None line predates the affix markup and has no
            // slot family to preserve, so it rerolls from the FULL pool instead of being bricked forever.
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test"); // Affix defaults to None
            item.AddAdditionalModifier(line);
            var provider = PoolProvider("Band",
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 100f, Affix = AffixKind.Prefix });

            string? rerolledId = CreateRecraftUpgrader(seed: 42, provider).TryRecraftModifier(item, line.InstanceId);

            Assert.IsNotNull(rerolledId);
            var rerolled = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId);
            Assert.AreEqual(AffixKind.Prefix, rerolled.Affix);
        }

        [TestMethod]
        public void TryRecraftModifier_GroupedLine_RemovesTheWholeGroupAndRollsOneCandidate()
        {
            // Parts of one composite roll present to the player as a SINGLE line (shared GroupId), so a
            // reroll targeted at the first part's id must take every part off and land ONE fresh atom.
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var partA = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Prefix, GroupId = "group-1" };
            var partB = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Prefix, GroupId = "group-1" };
            var keeper = new SimpleModifier(EntityParameter.Evade, ModifierValueType.Flat, 3f, "test") { Affix = AffixKind.Suffix };
            item.AddAdditionalModifier(partA);
            item.AddAdditionalModifier(partB);
            item.AddAdditionalModifier(keeper);
            var provider = PoolProvider("Band",
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 100f, Affix = AffixKind.Prefix });

            string? rerolledId = CreateRecraftUpgrader(seed: 42, provider).TryRecraftModifier(item, partA.InstanceId);

            Assert.IsNotNull(rerolledId);
            Assert.AreEqual(2, item.Modifiers.Count, "Expected: the untouched suffix line + exactly one fresh roll.");
            Assert.IsFalse(item.Modifiers.Any(modifier => modifier.InstanceId == partA.InstanceId || modifier.InstanceId == partB.InstanceId),
                "Every part of the group must leave the item.");
            Assert.IsTrue(item.Modifiers.Any(modifier => modifier.InstanceId == keeper.InstanceId), "Lines outside the group must survive.");
            Assert.AreEqual(EntityParameter.PhysicalDamage, item.Modifiers.Single(modifier => modifier.InstanceId == rerolledId).EntityParameter);
        }

        [TestMethod]
        public void TryRecraftModifier_GroupSpanningBothChannels_RemovesEntityAndContextParts()
        {
            var item = new EquipItem(EquipmentPiece.Ring, "Band", []);
            var entityPart = new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test") { Affix = AffixKind.Suffix, GroupId = "group-2" };
            var contextPart = new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.2f) { Affix = AffixKind.Suffix, GroupId = "group-2" };
            item.AddAdditionalModifier(entityPart);
            item.AddAdditionalContextModifier(contextPart);
            var provider = PoolProvider("Band",
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Flat, 100f, ModifierScope.Global) { Weight = 100f, Affix = AffixKind.Suffix });

            string? rerolledId = CreateRecraftUpgrader(seed: 7, provider).TryRecraftModifier(item, entityPart.InstanceId);

            Assert.IsNotNull(rerolledId);
            Assert.AreEqual(0, item.ContextModifiers.Count, "The group's context part must leave with the entity part.");
            Assert.AreEqual(rerolledId, item.Modifiers.Single().InstanceId);
        }

        [TestMethod]
        public void TryAscendItem_Gift_DrawsFromTheWholePoolOnWeightAlone()
        {
            // The mythic slot stands outside the rarity's prefix/suffix count, so no slot family is drawn:
            // every entry of the pool competes and weight alone decides. The heavy entry must always win.
            for (int seed = 0; seed < 30; seed++)
            {
                var item = CreateMaxedLegendary();
                var rnd = new DefaultRandomNumberGenerator(seed);
                var provider = new Mock<IItemDataProvider>();
                provider.Setup(mock => mock.GetEquipItemModifierPool("Mythic_Armor")).Returns(
                [
                    new ParameterDescriptor(EntityParameter.CriticalChance, ModifierValueType.Flat, 0.1f, ModifierScope.Global) { Weight = 1f, Affix = AffixKind.Mythic },
                    new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Increase, 0.4f, ModifierScope.Global) { Weight = 10000f, Affix = AffixKind.Mythic },
                ]);
                var ascender = new ItemAscender(rnd, provider.Object, new ModifierMaterializer(rnd), UnlockedMastery());

                var result = ascender.TryAscendItem(item);

                Assert.IsTrue(result.Succeeded);
                var gift = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == result.GiftedModifierIds.Single());
                Assert.AreEqual(EntityParameter.PhysicalDamage, gift.EntityParameter, $"seed {seed}: weight must decide the gift.");
                Assert.AreEqual(AffixKind.Mythic, gift.Affix, $"seed {seed}: the gift wears the mythic slot's family.");
            }
        }

        [TestMethod]
        public void TryAscendItem_GiftEntryMarkedAsAnOrdinaryAffix_StillLandsInTheMythicSlot()
        {
            // Data may forget the Mythic marker — everything drawn from a mythic pool belongs to that slot
            // anyway, so the ascender stamps it rather than trusting the entry.
            var item = CreateMaxedLegendary();
            var rnd = new DefaultRandomNumberGenerator(seed: 3);
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool("Mythic_Armor")).Returns(
            [
                new ParameterDescriptor(EntityParameter.PhysicalDamage, ModifierValueType.Increase, 0.4f, ModifierScope.Global) { Weight = 100f, Affix = AffixKind.Prefix },
            ]);
            var ascender = new ItemAscender(rnd, provider.Object, new ModifierMaterializer(rnd), UnlockedMastery());

            var result = ascender.TryAscendItem(item);

            Assert.IsTrue(result.Succeeded);
            var gift = (SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == result.GiftedModifierIds.Single());
            Assert.AreEqual(AffixKind.Mythic, gift.Affix);
        }

        [TestMethod]
        public void TryAscendItem_NotAscendable_RefusesWithoutGift()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []) { Rarity = Rarity.Legendary }; // level 0 < max

            var result = CreateAscender([]).TryAscendItem(item);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(0, result.GiftedModifierIds.Count);
            Assert.IsFalse(item.IsSealed);
        }

        private static ItemUpgrader CreateUpgrader() =>
            new(rnd: null!, Mock.Of<ICraftingMastery>(), EmptyPoolProvider().Object,
                Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(new DefaultRandomNumberGenerator(seed: 1)));

        /// <summary>Upgrader with a real seeded RNG — recraft paths under test DO roll. The provider is
        /// the live pool's source now, so recraft tests stub it instead of saving fodder on the item.</summary>
        private static ItemUpgrader CreateRecraftUpgrader(int seed, IItemDataProvider? itemDataProvider = null)
        {
            var rnd = new DefaultRandomNumberGenerator(seed);
            return new ItemUpgrader(rnd, Mock.Of<ICraftingMastery>(), itemDataProvider ?? EmptyPoolProvider().Object,
                Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(rnd));
        }

        /// <summary>Data provider whose item pool for <paramref name="itemId"/> is exactly
        /// <paramref name="pool"/> (family/resource pools stay empty).</summary>
        private static IItemDataProvider PoolProvider(string itemId, params IModifierDescriptor[] pool)
        {
            var provider = EmptyPoolProvider();
            provider.Setup(mock => mock.GetEquipItemModifierPool(itemId)).Returns(pool);
            return provider.Object;
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

        private static EquipItem CreateMaxedLegendary()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []) { Rarity = Rarity.Legendary };
            item.SetModifiers([new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 10f, "test")]);
            item.Upgrade(item.MaxUpdateLevel);
            return item;
        }

        /// <summary>Ascender whose gift ALWAYS rolls (rnd yields 0, so the kind roll picks Prefix)
        /// and always lands on the first pool entry. Items under test are helmets — Armor category.</summary>
        private static ItemAscender CreateAscender(List<IModifierDescriptor> mythicPool)
        {
            var rnd = new Mock<IRandomNumberGenerator>();
            rnd.Setup(mock => mock.RandFloat()).Returns(0f);
            rnd.Setup(mock => mock.RandFloatRange(It.IsAny<float>(), It.IsAny<float>())).Returns(0f);
            rnd.Setup(mock => mock.RandIntRange(It.IsAny<int>(), It.IsAny<int>())).Returns(7); // the extra-levels roll
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool("Mythic_Armor")).Returns(mythicPool);
            return new ItemAscender(rnd.Object, provider.Object, new ModifierMaterializer(rnd.Object), UnlockedMastery());
        }

        /// <summary>Mastery stub past the ascension gate with the shipped tuning shape; the gift
        /// always fires (chance 1) so gift-mechanics tests need no chance choreography.</summary>
        private static ICraftingMastery UnlockedMastery()
        {
            var mastery = new Mock<ICraftingMastery>();
            mastery.SetupGet(mock => mock.IsAscensionUnlocked).Returns(true);
            mastery.SetupGet(mock => mock.AscensionStatBonus).Returns(0.15f);
            mastery.Setup(mock => mock.GetMythicGiftChance()).Returns(1f);
            return mastery.Object;
        }
    }
}
