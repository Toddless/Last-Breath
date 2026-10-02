namespace LastBreathTest.Reputation
{
    using Core.Data.FactionData;
    using Core.Entity;
    using Core.Enums;
    using Core.Reputation;

    [TestClass]
    public class FactionRelationServiceTests
    {
        [TestMethod]
        public void RelationsAreDirected()
        {
            var service = CreateService();

            // Undead assault demons, demons stay neutral to undead — by design.
            Assert.IsTrue(service.IsHostile(Fractions.Undead, Fractions.Demon));
            Assert.IsFalse(service.IsHostile(Fractions.Demon, Fractions.Undead));
        }

        [TestMethod]
        public void DesignMatrixIsRespected()
        {
            var service = CreateService();

            Assert.AreEqual(RelationLevel.Hostility, service.GetRelation(Fractions.Dwarf, Fractions.Undead));
            Assert.AreEqual(RelationLevel.Dislike, service.GetRelation(Fractions.Dwarf, Fractions.Elf));
            Assert.AreEqual(RelationLevel.Neutral, service.GetRelation(Fractions.Dwarf, Fractions.Human));
            Assert.AreEqual(RelationLevel.Hostility, service.GetRelation(Fractions.Human, Fractions.Undead));
        }

        [TestMethod]
        public void SameFactionIsAllianceUnspecifiedIsNeutral()
        {
            var service = CreateService();

            Assert.AreEqual(RelationLevel.Alliance, service.GetRelation(Fractions.Human, Fractions.Human));
            Assert.AreEqual(RelationLevel.Neutral, service.GetRelation(Fractions.Animal, Fractions.Human));
            Assert.IsFalse(service.IsHostile(Fractions.Animal, Fractions.Human));
        }

        [TestMethod]
        public void DislikeIsNotHostile()
        {
            var service = CreateService();

            Assert.IsFalse(service.IsHostile(Fractions.Dwarf, Fractions.Elf));
        }

        [TestMethod]
        public void PlayerDefaultsResolveLevelsFromPoints()
        {
            var service = CreateService();

            // Undead start at -1000 = Hostility band: still attack on sight.
            Assert.AreEqual(-1000, service.GetReputation(Fractions.Undead));
            Assert.AreEqual(RelationLevel.Hostility, service.GetPlayerRelation(Fractions.Undead));
            Assert.IsTrue(service.IsHostileToPlayer(Fractions.Undead));

            Assert.AreEqual(500, service.GetReputation(Fractions.Human));
            Assert.AreEqual(RelationLevel.Neutral, service.GetPlayerRelation(Fractions.Human));

            Assert.AreEqual(0, service.GetReputation(Fractions.Elf));
            Assert.AreEqual(RelationLevel.Neutral, service.GetPlayerRelation(Fractions.Elf));
        }

        [TestMethod]
        public void AddReputationRaisesEventAndCrossesThresholds()
        {
            var service = CreateService();
            ReputationChangedArgs? changed = null;
            (Fractions Faction, RelationLevel Level)? levelChanged = null;
            service.PlayerReputationChanged += args => changed = args;
            service.PlayerRelationChanged += (faction, level) => levelChanged = (faction, level);

            service.AddReputation(Fractions.Elf, 1100, "TestDeed"); // Friendly from 1000, barrier 1050

            Assert.AreEqual(new ReputationChangedArgs(Fractions.Elf, 1100, 1100, "TestDeed"), changed);
            Assert.AreEqual((Fractions.Elf, RelationLevel.Friendly), levelChanged);
            Assert.AreEqual(RelationLevel.Friendly, service.GetPlayerRelation(Fractions.Elf));
        }

        [TestMethod]
        public void RisingWithinHysteresisBufferKeepsTheOldLevel()
        {
            var service = CreateService();

            service.AddReputation(Fractions.Elf, 1020, "TestDeed"); // past Friendly's 1000 but under the 1050 barrier

            Assert.AreEqual(RelationLevel.Neutral, service.GetPlayerRelation(Fractions.Elf));

            service.AddReputation(Fractions.Elf, 30, "TestDeed"); // 1050 beats the barrier
            Assert.AreEqual(RelationLevel.Friendly, service.GetPlayerRelation(Fractions.Elf));
        }

        [TestMethod]
        public void FallingWithinHysteresisBufferKeepsTheOldLevel()
        {
            var service = CreateService();
            service.AddReputation(Fractions.Elf, 1100, "TestDeed"); // Friendly

            service.AddReputation(Fractions.Elf, -120, "TestDeed"); // 980: below 1000 but above 950

            Assert.AreEqual(RelationLevel.Friendly, service.GetPlayerRelation(Fractions.Elf));

            service.AddReputation(Fractions.Elf, -40, "TestDeed"); // 940 < 950 — drops
            Assert.AreEqual(RelationLevel.Neutral, service.GetPlayerRelation(Fractions.Elf));
        }

        [TestMethod]
        public void ReputationIsClampedAndTopLevelIsReachable()
        {
            var service = CreateService();

            service.AddReputation(Fractions.Elf, 999999, "TestDeed");

            Assert.AreEqual(6000, service.GetReputation(Fractions.Elf));
            Assert.AreEqual(RelationLevel.Alliance, service.GetPlayerRelation(Fractions.Elf));

            service.AddReputation(Fractions.Elf, -999999, "TestDeed");
            Assert.AreEqual(-6000, service.GetReputation(Fractions.Elf));
            Assert.AreEqual(RelationLevel.Hatred, service.GetPlayerRelation(Fractions.Elf));
        }

        [TestMethod]
        public void FactionWithoutReputationIgnoresDeeds()
        {
            var service = CreateService();
            ReputationChangedArgs? changed = null;
            service.PlayerReputationChanged += args => changed = args;

            service.AddReputation(Fractions.Animal, -5000, "TestDeed");

            Assert.AreEqual(0, service.GetReputation(Fractions.Animal));
            Assert.IsNull(changed);
        }

        [TestMethod]
        public void FactionTraitsComeFromData()
        {
            var service = CreateService();

            Assert.IsTrue(service.HasReputation(Fractions.Elf));
            Assert.IsFalse(service.HasReputation(Fractions.Animal));
            Assert.IsTrue(service.CanRaid(Fractions.Elf));
            Assert.IsFalse(service.CanRaid(Fractions.Demon));
            Assert.IsFalse(service.CanRaid(Fractions.Animal));
        }

        [TestMethod]
        public void SetPlayerRelationJumpsToTheBandMidpointAndFlipsHostility()
        {
            var service = CreateService();
            (Fractions Faction, RelationLevel Level)? raised = null;
            service.PlayerRelationChanged += (faction, level) => raised = (faction, level);

            service.SetPlayerRelation(Fractions.Human, RelationLevel.Hostility);

            Assert.AreEqual((Fractions.Human, RelationLevel.Hostility), raised);
            Assert.AreEqual(-1400, service.GetReputation(Fractions.Human)); // (-2000 + -800) / 2
            Assert.IsTrue(service.IsHostileToPlayer(Fractions.Human));

            raised = null;
            service.SetPlayerRelation(Fractions.Human, RelationLevel.Hostility); // no change — no event
            Assert.IsNull(raised);
        }

        private static FactionRelationService CreateService() => new(FactionTestData.Create());
    }

    /// <summary>The design matrix and reputation scale as data — mirrors SharedData/Factions/FactionRelations.json.</summary>
    internal static class FactionTestData
    {
        public static FactionRelationsData Create() => new()
        {
            Relations =
            [
                Entry("Dwarf", "Undead", "Hostility"), Entry("Dwarf", "Elf", "Dislike"), Entry("Dwarf", "Human", "Neutral"),
                Entry("Elf", "Undead", "Hostility"), Entry("Elf", "Dwarf", "Dislike"), Entry("Elf", "Human", "Neutral"),
                Entry("Undead", "Human", "Hostility"), Entry("Undead", "Elf", "Hostility"),
                Entry("Undead", "Dwarf", "Hostility"), Entry("Undead", "Demon", "Hostility"),
                Entry("Human", "Undead", "Hostility"), Entry("Human", "Dwarf", "Neutral"), Entry("Human", "Elf", "Neutral"),
                Entry("Demon", "Dwarf", "Neutral"), Entry("Demon", "Elf", "Neutral"),
                Entry("Demon", "Human", "Neutral"), Entry("Demon", "Undead", "Neutral"),
            ],
            Reputation = new ReputationScaleData
            {
                Min = -6000,
                Max = 6000,
                Hysteresis = 50,
                Levels =
                [
                    Level("Hatred", -6000), Level("Hostility", -2000), Level("Dislike", -800), Level("Neutral", -200),
                    Level("Friendly", 1000), Level("Respect", 3000), Level("Alliance", 5000),
                ],
            },
            Factions =
            [
                Traits("Elf", hasReputation: true, canRaid: true), Traits("Dwarf", hasReputation: true, canRaid: true),
                Traits("Human", hasReputation: true, canRaid: true), Traits("Undead", hasReputation: true, canRaid: true),
                Traits("Demon", hasReputation: true, canRaid: false), Traits("Animal", hasReputation: false, canRaid: false),
                Traits("MysticalCreature", hasReputation: false, canRaid: false),
            ],
            PlayerDefaults =
            [
                new PlayerReputationEntry { Fraction = "Undead", Points = -1000 },
                new PlayerReputationEntry { Fraction = "Human", Points = 500 },
            ],
        };

        private static FactionRelationEntry Entry(string from, string to, string level) =>
            new() { From = from, To = to, Level = level };

        private static ReputationLevelEntry Level(string level, int from) =>
            new() { Level = level, From = from };

        private static FactionTraitsEntry Traits(string fraction, bool hasReputation, bool canRaid) =>
            new() { Fraction = fraction, HasReputation = hasReputation, CanRaid = canRaid };
    }
}
