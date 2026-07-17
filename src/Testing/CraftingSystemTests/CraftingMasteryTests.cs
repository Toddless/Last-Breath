namespace LastBreathTest.CraftingSystemTests
{
    using Core.Crafting;
    using Core.Data.GameData;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.MessageBus;
    using Core.Modifiers;
    using Core.Results;
    using Crafting.Source;
    using Moq;

    /// <summary>
    /// The mastery rework (design reference, 2026-07-16): levels 0..50 linearly lerp SIX bonus
    /// channels from 0 to their json maximums, tuning loads through the CraftingMastery catalog,
    /// and the value multiplier no longer punishes low mastery (level 0 crafts at x1.0 — a
    /// deliberate rebalance from the old x0.2 floor).
    /// </summary>
    [TestClass]
    public class CraftingMasteryTests
    {
        private const float Epsilon = 0.0001f;

        [TestMethod]
        public void ConfigLoadsFromJson_CurveAndCapComeFromData()
        {
            var mastery = CreateMastery();
            mastery.Apply(DataCatalog.CraftingMastery, new GameDataFile("test.json", """
            {
                "maxLevel": 10,
                "baseExp": 100,
                "expFactor": 1.0,
                "bonuses": { "upgradeChance": 1.0 }
            }
            """));

            Assert.AreEqual(10, mastery.MaximumLevel);
            Assert.AreEqual(100, mastery.ExpToNextLevelTotal()); // 100 * 1^1.0 — curve constants from json
            SetLevel(mastery, 10);
            Assert.AreEqual(1.0f, mastery.GetUpgradeChanceBonus(), Epsilon);
        }

        [TestMethod]
        public void SharedDataConfig_LoadsAndMatchesTheReference()
        {
            var mastery = CreateMastery();
            string json = File.ReadAllText(Path.Combine(FindSharedDataRoot(), "CraftingMastery", "CraftingMastery.json"));
            mastery.Apply(DataCatalog.CraftingMastery, new GameDataFile("CraftingMastery.json", json));

            Assert.AreEqual(50, mastery.MaximumLevel);
            Assert.AreEqual(40, mastery.GetExperienceReward(CraftingMode.Create, Rarity.Legendary));
            Assert.AreEqual(50, mastery.ExpToNextLevelTotal());
            // The ascension block ships with the reference numbers.
            Assert.AreEqual(0.15f, mastery.AscensionStatBonus, Epsilon);
            Assert.AreEqual(0.15f, mastery.GetMythicGiftChance(), Epsilon); // level 0: the bare base chance
        }

        [TestMethod]
        public void AscensionGate_OpensAt35_BonusLevelsCount()
        {
            var mastery = CreateMastery(); // defaults mirror the shipped json: gate 35

            SetLevel(mastery, 34);
            Assert.IsFalse(mastery.IsAscensionUnlocked);
            mastery.AddBonusLevel(); // 35 — equipment bonus levels count exactly like earned ones
            Assert.IsTrue(mastery.IsAscensionUnlocked);
        }

        [TestMethod]
        public void MythicGiftChance_IsBaseTimesChannelBonus()
        {
            var mastery = CreateMastery();

            Assert.AreEqual(0.15f, mastery.GetMythicGiftChance(), Epsilon);
            SetLevel(mastery, 25);
            Assert.AreEqual(0.15f * 1.5f, mastery.GetMythicGiftChance(), Epsilon); // channel at 50%
            SetLevel(mastery, 50);
            Assert.AreEqual(0.3f, mastery.GetMythicGiftChance(), Epsilon); // base × (1 + 100%)
        }

        [TestMethod]
        public void ExtraEffectChance_IsBaseTimesChannelBonus()
        {
            var mastery = CreateMastery(); // shipped defaults: base 0.1, extraEffect channel max 1.5

            Assert.AreEqual(0.1f, mastery.GetExtraEffectChance(), Epsilon);
            SetLevel(mastery, 25);
            Assert.AreEqual(0.1f * 1.75f, mastery.GetExtraEffectChance(), Epsilon); // channel at 75%
            SetLevel(mastery, 50);
            Assert.AreEqual(0.25f, mastery.GetExtraEffectChance(), Epsilon); // base × (1 + 150%)
        }

        [TestMethod]
        public void AllSixChannels_LerpLinearly_AtLevels0_25_50()
        {
            var mastery = CreateMastery(); // defaults mirror the shipped json: max 50, bonuses 2/1.5/1.5/1.5/1/1.5

            // Level 0: every channel at zero bonus, value multiplier at the NEUTRAL x1 (no penalty).
            Assert.AreEqual(0f, mastery.GetUpgradeChanceBonus(), Epsilon);
            Assert.AreEqual(1f, mastery.GetCurrentValueMultiplier(), Epsilon);
            Assert.AreEqual(0f, mastery.GetExtraEffectChanceBonus(), Epsilon);
            Assert.AreEqual(0f, mastery.GetMythicModifierChanceBonus(), Epsilon);
            Assert.AreEqual(0.3f, mastery.GetCurrentResourceMultiplier(), Epsilon); // base refund fraction

            SetLevel(mastery, 25); // halfway
            Assert.AreEqual(1.0f, mastery.GetUpgradeChanceBonus(), Epsilon);          // 0..200% -> 100%
            Assert.AreEqual(1.75f, mastery.GetCurrentValueMultiplier(), Epsilon);      // 1 + 0.75
            Assert.AreEqual(0.75f, mastery.GetExtraEffectChanceBonus(), Epsilon);      // 0..150% -> 75%
            Assert.AreEqual(0.5f, mastery.GetMythicModifierChanceBonus(), Epsilon);    // 0..100% -> 50%
            Assert.AreEqual(0.3f * 1.75f, mastery.GetCurrentResourceMultiplier(), Epsilon);

            SetLevel(mastery, 50); // cap
            Assert.AreEqual(2.0f, mastery.GetUpgradeChanceBonus(), Epsilon);
            Assert.AreEqual(2.5f, mastery.GetCurrentValueMultiplier(), Epsilon);
            Assert.AreEqual(1.5f, mastery.GetExtraEffectChanceBonus(), Epsilon);
            Assert.AreEqual(1.0f, mastery.GetMythicModifierChanceBonus(), Epsilon);
            Assert.AreEqual(0.3f * 2.5f, mastery.GetCurrentResourceMultiplier(), Epsilon);
        }

        [TestMethod]
        public void RarityChannel_ShiftsTheSplitTowardsRareWithLevel()
        {
            var mastery = CreateMastery();

            var atZero = mastery.GetRarityProbabilities();
            Assert.AreEqual(1f, atZero.Values.Sum(), Epsilon); // a probability split, always normalized
            Assert.AreEqual(0.01f, atZero[Rarity.Legendary], Epsilon); // level 0 = plain data weights (1/100)
            Assert.AreEqual(0.65f, atZero[Rarity.Uncommon], Epsilon);

            SetLevel(mastery, 50);
            var atCap = mastery.GetRarityProbabilities();
            Assert.AreEqual(1f, atCap.Values.Sum(), Epsilon);
            Assert.IsTrue(atCap[Rarity.Legendary] > atZero[Rarity.Legendary], "the rare end must grow with mastery");
            Assert.IsTrue(atCap[Rarity.Uncommon] < atZero[Rarity.Uncommon], "the common end must shrink with mastery");

            // The external rarityBonus channel stacks on top (its consumer — creation runes — comes later).
            var boosted = mastery.GetRarityProbabilities(rarityBonus: 5f);
            Assert.IsTrue(boosted[Rarity.Legendary] > atCap[Rarity.Legendary]);
        }

        [TestMethod]
        public void ExperienceRewards_ComeFromData_RarityTimesModeFactor()
        {
            var mastery = CreateMastery();

            Assert.AreEqual(40, mastery.GetExperienceReward(CraftingMode.Create, Rarity.Legendary));
            Assert.AreEqual(10, mastery.GetExperienceReward(CraftingMode.Create, Rarity.Common));
            Assert.AreEqual(9, mastery.GetExperienceReward(CraftingMode.Upgrade, Rarity.Epic));   // 30 x 0.3
            Assert.AreEqual(10, mastery.GetExperienceReward(CraftingMode.Shatter, Rarity.Rare));  // 20 x 0.5
            Assert.AreEqual(40, mastery.GetExperienceReward(CraftingMode.Ascend, Rarity.Legendary));
        }

        [TestMethod]
        public void UpgradeChance_ZeroMastery_KeepsTheBareCurve()
        {
            // Chance on a +0 item is P0=0.95: a roll just above it fails at zero mastery.
            var result = TryUpgrade(masteryLevel: 0, roll: 0.951f, out var item);

            Assert.AreEqual(ItemUpgradeResult.Failure, result);
            Assert.AreEqual(0, item.UpdateLevel);
        }

        [TestMethod]
        public void UpgradeChance_GrowsWithMastery_AndClampsAtOne()
        {
            // Max mastery: 0.95 x (1 + 2.0) clamps to a guaranteed 1.0 — even a roll of 1.0 succeeds.
            var result = TryUpgrade(masteryLevel: 50, roll: 1f, out var item);

            Assert.AreEqual(ItemUpgradeResult.Success, result);
            Assert.AreEqual(1, item.UpdateLevel);
        }

        [TestMethod]
        public void UpgradeChance_MidMastery_TurnsTheSameRollIntoSuccess()
        {
            // The identical roll that failed at zero mastery passes once the bonus lifts the curve.
            var result = TryUpgrade(masteryLevel: 25, roll: 0.951f, out var item);

            Assert.AreEqual(ItemUpgradeResult.Success, result);
            Assert.AreEqual(1, item.UpdateLevel);
        }

        [TestMethod]
        public void ExperienceCurve_LevelsUpFromJsonNumbers()
        {
            var mastery = CreateMastery();
            mastery.AddExperience(50); // exactly ExpToNextLevel(0->1) = 50 x 1^1.8

            Assert.AreEqual(1, mastery.CurrentLevel);
            Assert.AreEqual(0, mastery.CurrentExperience);
        }

        // ---- GetUpgradeChance: the informational mirror of the roll (the UI chance bar reads it) ----

        [TestMethod]
        public void GetUpgradeChance_ZeroMastery_IsTheBareCurve()
        {
            var upgrader = CreateUpgrader(masteryLevel: 0, roll: 0f);
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);

            Assert.AreEqual(0.95f, upgrader.GetUpgradeChance(item), Epsilon); // P0 straight off the curve
        }

        [TestMethod]
        public void GetUpgradeChance_GrowsWithMastery_AndClampsAtOne()
        {
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.Upgrade(6); // curve point below 0.5, so the mid-mastery product stays unclamped

            // +6: curve = lerp(P6=0.35, P9=0.15, 1/3); mastery 25 doubles it (bonus 100%).
            float bare = 0.35f + ((0.15f - 0.35f) / 3f);
            Assert.AreEqual(bare * 2f, CreateUpgrader(masteryLevel: 25, roll: 0f).GetUpgradeChance(item), Epsilon);

            // A fresh item at max mastery: 0.95 x 3 clamps to a guaranteed 1.
            var fresh = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            Assert.AreEqual(1f, CreateUpgrader(masteryLevel: 50, roll: 0f).GetUpgradeChance(fresh), Epsilon);
        }

        [TestMethod]
        public void GetUpgradeChance_FluxesAddOnTop_NonAdditivesAreIgnored()
        {
            var additives = new Mock<ICraftingAdditiveProvider>();
            additives.Setup(mock => mock.GetEffects("Flux")).Returns(new CraftingAdditiveEffects(0.1f, 0f, null));
            var upgrader = CreateUpgrader(masteryLevel: 0, roll: 0f, additives.Object);
            var item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            item.Upgrade(6);

            float bare = upgrader.GetUpgradeChance(item);
            // The full operation cost travels here (runes included) — only the flux moves the number.
            Assert.AreEqual(bare + 0.1f, upgrader.GetUpgradeChance(item, ["Flux", "Upgrade_Rune"]), Epsilon);
            Assert.AreEqual(bare, upgrader.GetUpgradeChance(item, ["Upgrade_Rune"]), Epsilon);
        }

        [TestMethod]
        public void GetUpgradeChance_IsExactlyWhatTryUpgradeRollsAgainst()
        {
            // One formula, one place: a roll AT the reported chance succeeds, a hair above it fails.
            foreach ((int masteryLevel, int level) in new[] { (0, 0), (0, 7), (25, 6), (10, 11) })
            {
                var probe = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
                probe.Upgrade(level);
                float chance = CreateUpgrader(masteryLevel, roll: 0f).GetUpgradeChance(probe);

                var exact = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
                exact.Upgrade(level);
                Assert.AreEqual(ItemUpgradeResult.Success,
                    CreateUpgrader(masteryLevel, roll: chance).TryUpgradeItem(exact),
                    $"mastery {masteryLevel}, +{level}: rolling the reported chance must succeed");

                var above = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
                above.Upgrade(level);
                Assert.AreEqual(ItemUpgradeResult.Failure,
                    CreateUpgrader(masteryLevel, roll: chance + 0.0005f).TryUpgradeItem(above),
                    $"mastery {masteryLevel}, +{level}: rolling above the reported chance must fail");
            }
        }

        private static ItemUpgradeResult TryUpgrade(int masteryLevel, float roll, out EquipItem item)
        {
            item = new EquipItem(EquipmentPiece.Helmet, "Crown", []);
            return CreateUpgrader(masteryLevel, roll).TryUpgradeItem(item);
        }

        private static ItemUpgrader CreateUpgrader(int masteryLevel, float roll, ICraftingAdditiveProvider? additives = null)
        {
            var mastery = CreateMastery();
            SetLevel(mastery, masteryLevel);

            var rnd = new Mock<IRandomNumberGenerator>();
            rnd.Setup(mock => mock.RandFloat()).Returns(roll);
            var provider = new Mock<Core.Data.IItemDataProvider>();
            return new ItemUpgrader(rnd.Object, mastery, provider.Object,
                additives ?? Mock.Of<ICraftingAdditiveProvider>(), new ModifierMaterializer(rnd.Object));
        }

        private static CraftingMastery CreateMastery() =>
            new(Mock.Of<IGameMessageBus>(), Mock.Of<IRandomNumberGenerator>());

        /// <summary>Bonus levels count into the progress factor exactly like earned ones — the
        /// cheapest deterministic way to park the mastery at a level.</summary>
        private static void SetLevel(CraftingMastery mastery, int level)
        {
            for (int i = 0; i < level; i++)
                mastery.AddBonusLevel();
        }

        private static string FindSharedDataRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "Data", "Shared");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }

            throw new InvalidOperationException($"Data/Shared symlink not found above {AppContext.BaseDirectory}");
        }
    }
}
