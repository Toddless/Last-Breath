namespace LastBreathTest.BattleSystemTests
{
    using Core.Data.GameData;
    using Core.Data.ReputationData;
    using Core.Enums;
    using Core.Reputation;
    using Newtonsoft.Json;

    [TestClass]
    public class ReputationPerkProviderTests
    {
        private FactionRelationService _relations = null!;
        private ReputationPerkProvider _perks = null!;

        [TestInitialize]
        public void Setup()
        {
            _relations = new FactionRelationService(FactionTestData.Create());
            _perks = new ReputationPerkProvider(_relations);
            _perks.Apply(DataCatalog.ReputationPerks, PerksFile());
        }

        [TestMethod]
        public void LevelsMapToTheirPerkSets()
        {
            var alliance = _perks.GetPerksForLevel(RelationLevel.Alliance);

            Assert.AreEqual(2, alliance.Count);
            Assert.IsTrue(alliance.Any(perk => perk is { Id: "Perk_Mythic_Gear_Access", Value: 1f }));
        }

        [TestMethod]
        public void LevelWithoutAnEntryGrantsNothing()
        {
            Assert.AreEqual(0, _perks.GetPerksForLevel(RelationLevel.Neutral).Count);
        }

        [TestMethod]
        public void FactionPerksFollowTheCurrentStanding()
        {
            Assert.AreEqual(0, _perks.GetPerks(Fractions.Elf).Count); // Neutral by default

            _relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Friendly);

            var perks = _perks.GetPerks(Fractions.Elf);
            Assert.AreEqual(1, perks.Count);
            Assert.AreEqual(new ReputationPerk("Perk_Price_Change", -0.15f), perks[0]);
        }

        private static GameDataFile PerksFile() => new("ReputationPerks.json", JsonConvert.SerializeObject(new ReputationPerksData
        {
            Levels =
            [
                new ReputationPerkLevelEntry
                {
                    Level = "Friendly",
                    Perks = [new ReputationPerkEntry { Id = "Perk_Price_Change", Value = -0.15f }],
                },
                new ReputationPerkLevelEntry
                {
                    Level = "Alliance",
                    Perks =
                    [
                        new ReputationPerkEntry { Id = "Perk_Price_Change", Value = -0.5f },
                        new ReputationPerkEntry { Id = "Perk_Mythic_Gear_Access", Value = 1f },
                    ],
                },
            ],
        }));
    }
}
