namespace LastBreathTest.BattleSystemTests
{
    using Battle.Internal.Npc;
    using Core.Data.FactionData;
    using Core.Enums;

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
        public void PlayerDefaultsApplyAndHatredIsHostile()
        {
            var service = CreateService();

            Assert.AreEqual(RelationLevel.Hatred, service.GetPlayerRelation(Fractions.Undead));
            Assert.IsTrue(service.IsHostileToPlayer(Fractions.Undead));
            Assert.AreEqual(RelationLevel.Neutral, service.GetPlayerRelation(Fractions.Human));
            Assert.IsFalse(service.IsHostileToPlayer(Fractions.Human));
        }

        [TestMethod]
        public void PlayerRelationChangeRaisesEventAndFlipsHostility()
        {
            var service = CreateService();
            (Fractions Faction, RelationLevel Level)? raised = null;
            service.PlayerRelationChanged += (faction, level) => raised = (faction, level);

            service.SetPlayerRelation(Fractions.Human, RelationLevel.Hostility);

            Assert.AreEqual((Fractions.Human, RelationLevel.Hostility), raised);
            Assert.IsTrue(service.IsHostileToPlayer(Fractions.Human));

            raised = null;
            service.SetPlayerRelation(Fractions.Human, RelationLevel.Hostility); // no change — no event
            Assert.IsNull(raised);
        }

        /// <summary>The design matrix as data — mirrors Battle/Data/Factions/FactionRelations.json.</summary>
        private static FactionRelationService CreateService() => new(new FactionRelationsData
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
            PlayerDefaults = [new PlayerRelationEntry { Fraction = "Undead", Level = "Hatred" }],
        });

        private static FactionRelationEntry Entry(string from, string to, string level) =>
            new() { From = from, To = to, Level = level };
    }
}
