namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Inventory;
    using Core.Items;
    using Core.Items.Grants;
    using Core.MessageBus;
    using Core.MessageBus.Requests;
    using Core.Modifiers;
    using Core.Save;
    using Crafting.Source;
    using Crafting.Source.RequestHandlers;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Ascension per the design reference (2026-07-16): gated by crafting mastery 35 (bonus levels
    /// count), then STRICTLY BEFORE the seal — the extra 1-69 sharpening roll, the flat +15% on
    /// everything (both line channels via AscensionMultiplier, grant payloads once), the category-bound
    /// mythic gift — and the Mythic transition last. All tuning lives in the CraftingMastery catalog.
    /// </summary>
    [TestClass]
    public class AscensionTests
    {
        [TestMethod]
        public void AscensionGate_Below35Refuses_At35Passes_BonusLevelsCount()
        {
            var item = MaxedLegendary();

            Assert.IsFalse(AscenderAt(MasteryAt(bonusLevels: 34)).CanAscend(item), "34 must sit below the gate.");
            Assert.IsTrue(AscenderAt(MasteryAt(bonusLevels: 35)).CanAscend(item), "35 is the gate.");

            // Earned and bonus levels mix: 34 earned + 1 equipment bonus crosses the gate.
            var mixed = MasteryAt(bonusLevels: 0, earnedLevels: 34);
            Assert.IsFalse(AscenderAt(mixed).CanAscend(item));
            mixed.AddBonusLevel();
            Assert.IsTrue(AscenderAt(mixed).CanAscend(item));
        }

        [TestMethod]
        public void TryAscendItem_BelowGate_LeavesTheItemUntouched()
        {
            var item = MaxedLegendary();

            var result = AscenderAt(MasteryAt(bonusLevels: 34)).TryAscendItem(item);

            Assert.IsFalse(result.Succeeded);
            Assert.IsFalse(item.IsSealed);
            Assert.AreEqual(Rarity.Legendary, item.Rarity);
            Assert.AreEqual(12, item.UpdateLevel);
            Assert.AreEqual(12, item.MaxUpdateLevel);
        }

        [TestMethod]
        public async Task Handler_BelowGate_SpendsNoResources()
        {
            var item = MaxedLegendary();
            var inventory = new Mock<IInventory>();
            inventory.Setup(mock => mock.GetItem<IEquipItem>(item.InstanceId)).Returns(item);
            inventory.Setup(mock => mock.GetTotalItemAmount(It.IsAny<string>())).Returns(99); // resources ARE there
            var handler = new AscendEquipItemRequestHandler(
                inventory.Object, AscenderAt(MasteryAt(bonusLevels: 34)), new CraftingResources(inventory.Object), Mock.Of<IGameMessageBus>());

            var result = await handler.HandleRequest(new AscendEquipItemRequest(item.InstanceId));

            Assert.IsFalse(result.Succeeded);
            inventory.Verify(mock => mock.RemoveItemById(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void FullFlow_SealsMythic_ScalesBothChannelsAndGrants()
        {
            var rnd = new DefaultRandomNumberGenerator(seed: 20260716);
            // Gift chance 0: the base flow is deterministic — extra levels are NOT part of ascension
            // itself (they are one of the rollable gift entries, covered separately below).
            var ascender = new ItemAscender(rnd, ArmorPoolProvider(DamagePoolEntry()), new ModifierMaterializer(rnd), GiftlessMastery());

            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []) { Rarity = Rarity.Legendary };
            var implicitLine = new SimpleModifier(EntityParameter.Armor, ModifierValueType.Flat, 100f, "test");
            var modifierLine = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 10f, "test");
            var contextLine = new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Increase, 0.2f);
            item.SetImplicits([implicitLine]);
            item.SetModifiers([modifierLine]);
            item.SetContextModifiers([contextLine]);
            item.AddGrant(new PassiveSkillGrant("grant_skill", "Skill_Regeneration", new Dictionary<string, float> { ["percent"] = 0.05f }, () => null));
            item.AddGrant(new ModifierGrant("grant_str", [new SimpleModifier(EntityParameter.Strength, ModifierValueType.Flat, 5f, "test")]));
            item.Upgrade(item.MaxUpdateLevel); // fully sharpened +12, multiplier 2.2

            var result = ascender.TryAscendItem(item);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(Rarity.Mythic, item.Rarity);
            Assert.IsTrue(item.IsSealed);

            // Ascension itself adds NO levels: the item stays at its full 12.
            Assert.AreEqual(12, item.MaxUpdateLevel);
            Assert.AreEqual(item.MaxUpdateLevel, item.UpdateLevel, "An ascended item is fully sharpened by definition.");

            // Every FLAT line of BOTH channels = Base × UpdateMultiplier × 1.15 (implicits included).
            float updateMultiplier = 1f + (12 * 0.05f);
            Assert.AreEqual(100f * updateMultiplier * 1.15f, implicitLine.Value, 0.01f);
            Assert.AreEqual(10f * updateMultiplier * 1.15f, modifierLine.Value, 0.01f);

            // A percent line already multiplies values the scales have raised: scaling it too would stack a
            // multiplier on a multiplier, so sharpening and ascension leave it exactly as data wrote it.
            Assert.AreEqual(0.2f, contextLine.Value, 0.001f);
            Assert.AreEqual(1.15f, item.AscensionMultiplier, 0.0001f);

            // Grant payloads scaled ONCE: the +15% becomes their new base.
            var skillGrant = (PassiveSkillGrant)item.Grants.First(grant => grant.Id == "grant_skill");
            Assert.AreEqual(0.05f * 1.15f, skillGrant.Properties["percent"], 0.0001f);
            var modifierGrant = (ModifierGrant)item.Grants.First(grant => grant.Id == "grant_str");
            Assert.AreEqual(5f * 1.15f, modifierGrant.Modifiers.Single().BaseValue, 0.0001f);

            // Sealed forever: no further sharpening, no line mutations.
            Assert.IsFalse(item.Upgrade());
            item.RemoveAdditionalModifier(modifierLine.InstanceId);
            Assert.IsTrue(item.Modifiers.Any(modifier => modifier.InstanceId == modifierLine.InstanceId), "A sealed item must refuse line removal.");
        }

        [TestMethod]
        public void MythicGift_FrequencyGrowsWithMastery()
        {
            // Same seeds, higher chance threshold: a gift at 35 implies a gift at 50 (monotone),
            // and the seeds landing between the two chances make the count strictly larger.
            int giftsAt35 = CountGifts(masteryLevel: 35, seeds: 600);
            int giftsAt50 = CountGifts(masteryLevel: 50, seeds: 600);

            Assert.IsTrue(giftsAt35 > 0, "The gift never fired at the gate level — the chance path looks dead.");
            Assert.IsTrue(giftsAt50 > giftsAt35, $"Gift count must grow with mastery: {giftsAt35} at 35 -> {giftsAt50} at 50.");
        }

        [TestMethod]
        public void MythicGift_ExtraLevelsEntry_RaisesTheCapBeforeTheSeal()
        {
            var rnd = new DefaultRandomNumberGenerator(seed: 5);
            var levelsEntry = new UpgradeLevelsDescriptor(1, 69) { Weight = 100f, Affix = AffixKind.Prefix };
            var ascender = new ItemAscender(rnd, ArmorPoolProvider(levelsEntry), new ModifierMaterializer(rnd), AlwaysGiftingMastery());
            var item = MaxedLegendary();

            var result = ascender.TryAscendItem(item);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.GiftedModifierIds.Count, "A levels gift lands as levels, not lines.");
            int extra = item.MaxUpdateLevel - 12;
            Assert.IsTrue(extra is >= 1 and <= 69, $"Extra levels {extra} escaped the entry bounds 1..69.");
            Assert.AreEqual(item.MaxUpdateLevel, item.UpdateLevel, "Re-sharpened to the raised cap before the seal.");
            Assert.IsTrue(item.IsSealed);
            // The lines rode the standard upgrade path: Base × (1 + levels × 5%) × the ascension 1.15.
            float expected = 10f * (1f + (item.UpdateLevel * 0.05f)) * 1.15f;
            Assert.AreEqual(expected, item.Modifiers.Single().Value, 0.01f);
        }

        [TestMethod]
        public void FlagLine_IgnoresSharpeningAndAscension()
        {
            // "Attacks ignore elemental resistances" is a switch: there is no number to grow, so neither
            // the sharpening scale nor the ascension +15% may touch its value channel.
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []) { Rarity = Rarity.Legendary };
            var flag = new ContextModifierEntry(ContextParameter.HealingEfficiency, ModifierValueType.Flag, 1f);
            var line = new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 10f, "test");
            item.AddAdditionalContextModifier(flag);
            item.AddAdditionalModifier(line);

            item.Upgrade(item.MaxUpdateLevel);
            item.AscensionMultiplier = 1.15f;

            Assert.AreEqual(1f, flag.Value, 0.001f, "A flag line must stay exactly as data wrote it.");
            Assert.AreEqual(10f * 1.6f * 1.15f, line.Value, 0.01f, "The ordinary line still rides the full formula.");
        }

        [TestMethod]
        public void MythicGift_GrantEntry_LandsAsAGrantNotALine()
        {
            // The pool may hold behaviour no line can express ("ignores the first damage taken each turn").
            var rnd = new DefaultRandomNumberGenerator(seed: 5);
            var entry = new GrantDescriptor(GrantKind.Passive, "Passive_Skill_Regeneration", new Dictionary<string, float> { ["percent"] = 0.05f })
                { Weight = 100f, Affix = AffixKind.Prefix };
            var granted = Mock.Of<IItemGrant>(mock => mock.Id == "Passive_Skill_Regeneration");
            var factory = new Mock<IGrantFactory>();
            factory.Setup(mock => mock.Create(GrantKind.Passive, "Passive_Skill_Regeneration", It.IsAny<List<IModifier>>(), It.IsAny<IReadOnlyDictionary<string, float>>()))
                .Returns(granted);
            var ascender = new ItemAscender(rnd, ArmorPoolProvider(entry), new ModifierMaterializer(rnd, factory.Object), AlwaysGiftingMastery());
            var item = MaxedLegendary();
            int linesBefore = item.Modifiers.Count;

            var result = ascender.TryAscendItem(item);

            Assert.IsTrue(result.Succeeded);
            Assert.AreSame(granted, item.Grants.Single());
            Assert.AreEqual(linesBefore, item.Modifiers.Count, "A grant gift must not add a line.");
            Assert.AreEqual(0, result.GiftedModifierIds.Count, "There is no line id to report for a grant gift.");
        }

        [TestMethod]
        public void MythicGift_IsStampedWithItsOrigin_AndSurvivesTheSave()
        {
            // The gift entry rolled from the Prefix half of the pool, but what lands on the item is a
            // MYTHIC line — that stamp is what earns it its own slot in the tooltip, and it rides the
            // ordinary affix field, so the save must bring it back.
            var rnd = new DefaultRandomNumberGenerator(seed: 5);
            var entry = new ParameterDescriptor(EntityParameter.Health, ModifierValueType.Multiplicative, 0.3f, ModifierScope.Global)
                { Weight = 100f, Affix = AffixKind.Prefix };
            var ascender = new ItemAscender(rnd, ArmorPoolProvider(entry), new ModifierMaterializer(rnd), AlwaysGiftingMastery());
            var item = MaxedLegendary();

            var result = ascender.TryAscendItem(item);

            Assert.IsTrue(result.Succeeded);
            string giftId = result.GiftedModifierIds.Single();
            Assert.AreEqual(AffixKind.Mythic, ((SimpleModifier)item.Modifiers.Single(modifier => modifier.InstanceId == giftId)).Affix);

            var converter = new EquipItemSaveConverter(new GrantFactory(() => null, () => null, () => null));
            var restored = converter.FromData(converter.ToData(item));

            Assert.AreEqual(1, restored.Modifiers.Count(modifier => (modifier as SimpleModifier)?.Affix == AffixKind.Mythic),
                "The gift must come back from the save still wearing its mythic stamp.");
        }

        [TestMethod]
        public void GiftPool_FollowsTheItemCategory_WeaponNeverDrawsArmorLines()
        {
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetEquipItemModifierPool("Mythic_Weapon")).Returns([DamagePoolEntry()]);
            provider.Setup(mock => mock.GetEquipItemModifierPool("Mythic_Armor")).Returns(
                [new ParameterDescriptor(EntityParameter.Health, ModifierValueType.Multiplicative, 0.3f, ModifierScope.Global) { Weight = 10000f, Affix = AffixKind.Prefix }]);

            for (int seed = 0; seed < 30; seed++)
            {
                var rnd = new DefaultRandomNumberGenerator(seed);
                var weapon = new WeaponItem(WeaponType.Sword, Handedness.OneHanded, 100f, 0.1f, 1.5f, "Sword", []) { Rarity = Rarity.Legendary };
                weapon.Upgrade(weapon.MaxUpdateLevel);

                var result = new ItemAscender(rnd, provider.Object, new ModifierMaterializer(rnd), AlwaysGiftingMastery()).TryAscendItem(weapon);

                Assert.IsTrue(result.Succeeded);
                Assert.IsTrue(result.GiftedModifierIds.Count > 0, $"seed {seed}: chance 1 must always gift.");
                foreach (string giftId in result.GiftedModifierIds)
                    Assert.AreEqual(EntityParameter.PhysicalDamage, weapon.Modifiers.First(modifier => modifier.InstanceId == giftId).EntityParameter,
                        $"seed {seed}: a weapon drew a line from a foreign category pool.");
            }

            provider.Verify(mock => mock.GetEquipItemModifierPool("Mythic_Armor"), Times.Never);
            provider.Verify(mock => mock.GetEquipItemModifierPool("Mythic_Weapon"), Times.AtLeastOnce);
        }

        [TestMethod]
        public void EveryCategoryMappedPoolExistsInShippedData()
        {
            var shippedPools = ParseShippedMythicPools();
            var requested = new List<string>();
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool(It.IsAny<string>()))
                .Callback((string id) => requested.Add(id))
                .Returns([]);

            // One item per category — the ascender must ask for each category's pool by name.
            IEquipItem[] items =
            [
                Maxed(new WeaponItem(WeaponType.Sword, Handedness.OneHanded, 100f, 0.1f, 1.5f, "Sword", []) { Rarity = Rarity.Legendary }),
                MaxedLegendary(EquipmentPiece.Helmet),
                MaxedLegendary(EquipmentPiece.Ring),
            ];
            foreach (var item in items)
            {
                var rnd = new DefaultRandomNumberGenerator(seed: 1);
                new ItemAscender(rnd, provider.Object, new ModifierMaterializer(rnd), AlwaysGiftingMastery()).TryAscendItem(item);
            }

            var mappedPoolIds = requested.Distinct().ToList();
            Assert.AreEqual(3, mappedPoolIds.Count, "Every equipment category must map to its own mythic pool.");
            foreach (string poolId in mappedPoolIds)
                Assert.IsTrue(shippedPools.ContainsKey(poolId), $"The ascender asks for pool '{poolId}' which the shipped data does not define.");
        }

        [TestMethod]
        public void ShippedMythicPools_EveryEntryParsesWithAnAffix()
        {
            string json = File.ReadAllText(Path.Combine(SharedDataRoot(), "ModifierPools", "MythicModifiers.json"));
            var pools = ParseShippedMythicPools();

            var rawCounts = JObject.Parse(json)["pools"]!
                .ToDictionary(pool => (string)pool["id"]!, pool => ((JArray)pool["modifiersPool"]!).Count);
            foreach (string poolId in (string[])["Mythic_Weapon", "Mythic_Jewellery", "Mythic_Armor"])
            {
                Assert.IsTrue(pools.ContainsKey(poolId), $"Pool '{poolId}' is missing from the shipped data.");
                Assert.AreEqual(rawCounts[poolId], pools[poolId].Count,
                    $"'{poolId}': an entry was dropped by the parser (bad affix or parameter).");
                Assert.IsTrue(pools[poolId].Count >= 3, $"'{poolId}' holds {pools[poolId].Count} entries — the reference wants 3-4.");
                Assert.IsTrue(pools[poolId].All(descriptor => descriptor.Affix != AffixKind.None),
                    $"'{poolId}': every rollable entry must claim a slot family.");
            }
        }

        [TestMethod]
        public void MarksExistInResources_AscendCosts_AndLootTables()
        {
            string[] marks = ["Upgrade_Resource_Weaponsmith_Mark", "Upgrade_Resource_Jeweler_Mark", "Upgrade_Resource_Armorsmith_Mark"];
            string root = SharedDataRoot();

            // 1. The resources catalog ships every mark as a Mythic "Mark"-tagged upgrade resource.
            var resources = JObject.Parse(File.ReadAllText(Path.Combine(root, "Resources", "CraftingResources.json")));
            var upgradeResources = ((JArray)resources["upgradeResources"]!)
                .ToDictionary(entry => (string)entry["id"]!, entry => entry);
            foreach (string mark in marks)
            {
                Assert.IsTrue(upgradeResources.ContainsKey(mark), $"{mark} is missing from CraftingResources.");
                Assert.AreEqual("Mythic", (string?)upgradeResources[mark]["rarity"], $"{mark} must be Mythic (reference).");
                Assert.IsTrue(upgradeResources[mark]["tags"]!.Values<string>().Contains("Mark"), $"{mark} must carry the Mark tag.");
            }

            // 2. The ascend price is exactly ONE mark per category (the "10 runes" placeholder is dead).
            var costs = JObject.Parse(File.ReadAllText(Path.Combine(root, "UpgradeCosts", "UpgradeCosts.json")));
            var ascendRequirements = ((JArray)costs["ascend"]!)
                .SelectMany(category => (JArray)category["requirements"]!)
                .ToList();
            Assert.AreEqual(3, ascendRequirements.Count);
            foreach (var requirement in ascendRequirements)
            {
                Assert.IsTrue(marks.Contains((string?)requirement["id"]), $"Ascend cost references a non-mark resource: {requirement["id"]}.");
                Assert.AreEqual(1, (int)requirement["amount"]!, "The ascension price is one mark.");
            }

            // 3. Every mark is obtainable: present in the loot tables (general tier 0 by decision).
            var lootIds = JObject.Parse(File.ReadAllText(Path.Combine(root, "LootTables", "LootTables.json")))
                .SelectTokens("$..items[*].id")
                .Select(token => (string?)token)
                .ToHashSet();
            foreach (string mark in marks)
                Assert.IsTrue(lootIds.Contains(mark), $"{mark} is not dropped by any loot table.");
        }

        private static Dictionary<string, List<IModifierDescriptor>> ParseShippedMythicPools()
        {
            string json = File.ReadAllText(Path.Combine(SharedDataRoot(), "ModifierPools", "MythicModifiers.json"));
            return new DataParser(new LootGeneration.Internal.ItemGameDataFactory()).ParseEquipItemModifierPools(json);
        }

        private static int CountGifts(int masteryLevel, int seeds)
        {
            int gifts = 0;
            var provider = ArmorPoolProvider(DamagePoolEntry());
            for (int seed = 0; seed < seeds; seed++)
            {
                var rnd = new DefaultRandomNumberGenerator(seed);
                var ascender = new ItemAscender(rnd, provider, new ModifierMaterializer(rnd), MasteryAt(bonusLevels: masteryLevel));
                if (ascender.TryAscendItem(MaxedLegendary()).GiftedModifierIds.Count > 0) gifts++;
            }

            return gifts;
        }

        /// <summary>Real mastery on the shipped defaults (gate 35, roll 1..69, +15%, gift base 0.15),
        /// parked at bonus + earned levels.</summary>
        private static CraftingMastery MasteryAt(int bonusLevels, int earnedLevels = 0)
        {
            var mastery = new CraftingMastery(Mock.Of<IGameMessageBus>(), Mock.Of<IRandomNumberGenerator>());
            for (int i = 0; i < bonusLevels; i++) mastery.AddBonusLevel();
            while (mastery.CurrentLevel < earnedLevels) mastery.AddExperience(mastery.ExpToNextLevelRemain());
            return mastery;
        }

        /// <summary>Mastery stub past the gate whose gift ALWAYS fires — for gift-mechanics tests.</summary>
        private static ICraftingMastery AlwaysGiftingMastery()
        {
            var mastery = new Mock<ICraftingMastery>();
            mastery.SetupGet(mock => mock.IsAscensionUnlocked).Returns(true);
            mastery.SetupGet(mock => mock.AscensionStatBonus).Returns(0.15f);
            mastery.Setup(mock => mock.GetMythicGiftChance()).Returns(1f);
            return mastery.Object;
        }

        /// <summary>Mastery stub past the gate whose gift NEVER fires — the deterministic base flow.</summary>
        private static ICraftingMastery GiftlessMastery()
        {
            var mastery = new Mock<ICraftingMastery>();
            mastery.SetupGet(mock => mock.IsAscensionUnlocked).Returns(true);
            mastery.SetupGet(mock => mock.AscensionStatBonus).Returns(0.15f);
            mastery.Setup(mock => mock.GetMythicGiftChance()).Returns(0f);
            return mastery.Object;
        }

        private static ItemAscender AscenderAt(ICraftingMastery mastery)
        {
            var rnd = new DefaultRandomNumberGenerator(seed: 1);
            return new ItemAscender(rnd, ArmorPoolProvider(DamagePoolEntry()), new ModifierMaterializer(rnd), mastery);
        }

        private static IItemDataProvider ArmorPoolProvider(params IModifierDescriptor[] pool)
        {
            var provider = new Mock<IItemDataProvider>();
            provider.Setup(mock => mock.GetEquipItemModifierPool(It.IsAny<string>())).Returns([]);
            provider.Setup(mock => mock.GetEquipItemModifierPool("Mythic_Armor")).Returns(pool);
            return provider.Object;
        }

        private static ParameterDescriptor DamagePoolEntry() =>
            new(EntityParameter.PhysicalDamage, ModifierValueType.Increase, 0.4f, ModifierScope.Global) { Weight = 100f, Affix = AffixKind.Prefix };

        private static EquipItem MaxedLegendary(EquipmentPiece piece = EquipmentPiece.Helmet)
        {
            var item = new EquipItem(piece, "Crown", []) { Rarity = Rarity.Legendary };
            item.SetModifiers([new SimpleModifier(EntityParameter.Intelligence, ModifierValueType.Flat, 10f, "test")]);
            return Maxed(item);
        }

        private static EquipItem Maxed(EquipItem item)
        {
            item.Upgrade(item.MaxUpdateLevel);
            return item;
        }

        private static string SharedDataRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "SharedData")))
                directory = directory.Parent;
            Assert.IsNotNull(directory, "SharedData not found above the test bin directory");
            return Path.Combine(directory.FullName, "SharedData");
        }
    }
}
