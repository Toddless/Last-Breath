namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle;
    using Core.Data.GameData;
    using Core.Data.SaveData;
    using Core.MessageBus;
    using Core.PassiveTree.Allocation;
    using Core.Save.Participants;
    using Moq;

    /// <summary>
    /// Martial art mastery counts from ZERO: fifty level ups, fifty passive tree points. The tests
    /// pin the scale (start, clamps, session reset), the curve (indexed by the level being bought,
    /// so the first one is never free), the base level the save carries (equipment bonus levels must
    /// never be earned into it) and the point budget the tree receives.
    /// </summary>
    [TestClass]
    public class MartialArtMasteryTests
    {
        [TestMethod]
        public void FreshMastery_StartsAtLevelZero()
        {
            var mastery = CreateMastery();

            Assert.AreEqual(0, mastery.CurrentLevel);
            Assert.AreEqual(0, mastery.CurrentExperience);
            Assert.AreEqual(0, mastery.BonusLevel);
        }

        [TestMethod]
        public void FirstLevelUp_CostsTheBaseExperience_NotNothing()
        {
            var mastery = CreateMastery();
            int cost = mastery.ExpToNextLevelTotal();

            Assert.IsTrue(cost > 0, "a free first level would be handed out by the first kill of the game");

            mastery.AddExperience(cost - 1);
            Assert.AreEqual(0, mastery.CurrentLevel, "one experience short of the cost must not level");

            mastery.AddExperience(1);
            Assert.AreEqual(1, mastery.CurrentLevel);
            Assert.AreEqual(0, mastery.CurrentExperience);
        }

        [TestMethod]
        public void LevelUps_FromZeroToTheCap_AreExactlyFifty()
        {
            var mastery = CreateMastery();
            int levelUps = 0;

            while (mastery.CurrentLevel < mastery.MaximumLevel && levelUps <= mastery.MaximumLevel * 2)
            {
                mastery.AddExperience(mastery.ExpToNextLevelTotal());
                levelUps++;
            }

            Assert.AreEqual(50, mastery.MaximumLevel);
            Assert.AreEqual(mastery.MaximumLevel, mastery.CurrentLevel);
            Assert.AreEqual(50, levelUps, "zero to the cap must take exactly one level up per level");
        }

        [TestMethod]
        public void Curve_ChargesMoreForEveryNextLevel_AndNeverNothing()
        {
            var mastery = CreateMastery();
            int previous = 0;

            for (int level = 0; level < mastery.MaximumLevel; level++)
            {
                mastery.RestoreState(level, 0);
                int cost = mastery.ExpToNextLevelTotal();

                Assert.IsTrue(cost > previous,
                    $"level {level} costs {cost} while the level before it cost {previous} — the curve must grow with every step");
                previous = cost;
            }
        }

        [TestMethod]
        public void RestoreState_ClampsIntoTheZeroToCapRange()
        {
            var mastery = CreateMastery();

            mastery.RestoreState(-5, -100);
            Assert.AreEqual(0, mastery.CurrentLevel);
            Assert.AreEqual(0, mastery.CurrentExperience);

            mastery.RestoreState(500, 10);
            Assert.AreEqual(mastery.MaximumLevel, mastery.CurrentLevel);
        }

        [TestMethod]
        public void LegacySave_WithTheOldLevelOneBase_LoadsOntoTheNewScale()
        {
            var mastery = CreateMastery();

            mastery.RestoreState(1, 0); // saves written before the zero-based scale start here

            Assert.AreEqual(1, mastery.CurrentLevel);
            Assert.IsTrue(mastery.ExpToNextLevelTotal() > 0);
        }

        [TestMethod]
        public void ResetSession_ReturnsToTheZeroOfAFreshGame()
        {
            var mastery = CreateMastery();
            mastery.AddExperience(10_000);
            mastery.AddBonusLevel();
            Assert.IsTrue(mastery.CurrentLevel > 0, "the setup must actually level up");

            mastery.ResetSession();

            Assert.AreEqual(0, mastery.CurrentLevel);
            Assert.AreEqual(0, mastery.CurrentExperience);
            Assert.AreEqual(0, mastery.BonusLevel);
        }

        [TestMethod]
        public void LevelUp_DoesNotInflateTheSavedBaseLevel()
        {
            var mastery = CreateMastery();
            mastery.AddBonusLevel();
            mastery.AddBonusLevel();
            mastery.AddBonusLevel(); // equipment grants: worn levels are never earned into the base
            int before = SavedBaseLevel(mastery);

            mastery.AddExperience(mastery.ExpToNextLevelTotal());

            Assert.AreEqual(before + 1, SavedBaseLevel(mastery), "one level up must move the saved base by exactly one");
            Assert.AreEqual(3, mastery.BonusLevel);
        }

        [TestMethod]
        public void CurrentLevelChange_ReportsTheLevelTheGetterShows()
        {
            var mastery = CreateMastery();
            mastery.AddBonusLevel();
            int reported = -1;
            mastery.CurrentLevelChange += level => reported = level;

            mastery.AddExperience(mastery.ExpToNextLevelTotal());

            Assert.AreEqual(mastery.CurrentLevel, reported, "the event must carry the level the subscribers read");
        }

        [TestMethod]
        public void Curve_ComesFromTheCatalog_NotFromConstants()
        {
            var mastery = CreateMastery();

            mastery.Apply(DataCatalog.MartialArtMastery, new GameDataFile("test.json", """
            { "maxLevel": 3, "baseExp": 100, "expFactor": 1.0 }
            """));

            Assert.AreEqual(3, mastery.MaximumLevel);
            Assert.AreEqual(100, mastery.ExpToNextLevelTotal()); // 100 x 1^1
            mastery.AddExperience(100);
            Assert.AreEqual(200, mastery.ExpToNextLevelTotal()); // 100 x 2^1
        }

        [TestMethod]
        public void ShippedCatalog_LoadsAndCarriesTheReferenceCurve()
        {
            var mastery = CreateMastery();
            string json = File.ReadAllText(Path.Combine(FindSharedDataRoot(), "MartialArtMastery", "MartialArtMastery.json"));

            mastery.Apply(DataCatalog.MartialArtMastery, new GameDataFile("MartialArtMastery.json", json));

            Assert.AreEqual(50, mastery.MaximumLevel);
            Assert.AreEqual(50, mastery.ExpToNextLevelTotal()); // 50 x 1^1.8 — the first level costs the base
        }

        [TestMethod]
        public void BrokenCatalogNumbers_FallBackToTheDefaults_NotToASilentZero()
        {
            var mastery = CreateMastery();

            mastery.Apply(DataCatalog.MartialArtMastery, new GameDataFile("broken.json", """
            { "maxLevel": 0, "baseExp": 0, "expFactor": 0 }
            """));

            Assert.AreEqual(50, mastery.MaximumLevel);
            Assert.AreEqual(50, mastery.ExpToNextLevelTotal(), "a zero curve would hand out every level at once");
        }

        [TestMethod]
        public void EveryLevelUp_GrantsOnePassiveTreePoint()
        {
            var tree = new TreePointsSpy();
            var mastery = CreateMastery(tree.Service);

            mastery.AddExperience(mastery.ExpToNextLevelTotal());
            Assert.AreEqual(1, tree.TotalPoints);

            mastery.AddExperience(mastery.ExpToNextLevelTotal());
            Assert.AreEqual(2, tree.TotalPoints);
        }

        [TestMethod]
        public void TheWholeCurve_GrantsExactlyFiftyPoints()
        {
            var tree = new TreePointsSpy();
            var mastery = CreateMastery(tree.Service);

            while (mastery.CurrentLevel < mastery.MaximumLevel)
                mastery.AddExperience(mastery.ExpToNextLevelTotal());

            Assert.AreEqual(50, tree.TotalPoints, "fifty level ups are the whole tree budget mastery ever pays");
        }

        [TestMethod]
        public void LoadingASave_DoesNotHandTheBudgetOutTwice()
        {
            var tree = new TreePointsSpy();
            var mastery = CreateMastery(tree.Service);

            mastery.RestoreState(mastery.MaximumLevel, 0);
            mastery.RestoreState(mastery.MaximumLevel, 0); // a second load inside the same session

            Assert.AreEqual(50, tree.TotalPoints, "the total is stated, not added — a reload must not duplicate the budget");
        }

        [TestMethod]
        public void EquipmentBonusLevels_DoNotGrantPoints()
        {
            var tree = new TreePointsSpy();
            var mastery = CreateMastery(tree.Service);
            mastery.AddExperience(mastery.ExpToNextLevelTotal());

            mastery.AddBonusLevel();
            mastery.AddBonusLevel();

            Assert.AreEqual(3, mastery.CurrentLevel);
            Assert.AreEqual(1, tree.TotalPoints, "the budget is earned, not worn");
        }

        /// <summary>The number the save file carries: the earned base, without equipment bonus levels.</summary>
        private static int SavedBaseLevel(IMartialArtMastery mastery) =>
            new MasterySaveParticipant(mastery).Capture().ToObject<MasterySaveData>()!.BaseLevel;

        private static MartialArtMastery CreateMastery(IPassiveTreeService? tree = null) =>
            new(Mock.Of<IGameMessageBus>(), () => tree);

        /// <summary>The shared data root the shipped catalogs live in (Data/Shared symlink).</summary>
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

        /// <summary>Records what mastery states as the tree point total.</summary>
        private sealed class TreePointsSpy
        {
            public TreePointsSpy()
            {
                var mock = new Mock<IPassiveTreeService>();
                mock.Setup(tree => tree.SetTotalPoints(It.IsAny<int>()))
                    .Callback((int points) => TotalPoints = points);
                Service = mock.Object;
            }

            public IPassiveTreeService Service { get; }

            public int TotalPoints { get; private set; }
        }
    }
}
