namespace LastBreathTest.Reputation
{
    using Core.Enums;
    using Core.Events;
    using Core.Reputation;
    using Core.Save.Participants;
    using Core.Services;
    using Godot;

    [TestClass]
    public class PersonalReputationServiceTests
    {
        private GameEventBus _bus = null!;
        private FactionRelationService _factions = null!;
        private PersonalReputationService _personal = null!;

        [TestInitialize]
        public void Setup()
        {
            _bus = new GameEventBus();
            _factions = new FactionRelationService(FactionTestData.Create());
            _personal = new PersonalReputationService(_factions, _bus);
        }

        [TestMethod]
        public void PointsTranslateIntoLadderShifts()
        {
            // Defaults: 300 points per shift, max ±2.
            _personal.SetPersonal("npc-1", 299);
            Assert.AreEqual(0, _personal.GetLevelShift("npc-1"));

            _personal.SetPersonal("npc-1", 300);
            Assert.AreEqual(1, _personal.GetLevelShift("npc-1"));

            _personal.SetPersonal("npc-1", 1000); // 1000/300 = 3, capped at 2
            Assert.AreEqual(2, _personal.GetLevelShift("npc-1"));

            _personal.SetPersonal("npc-1", -300);
            Assert.AreEqual(-1, _personal.GetLevelShift("npc-1"));
        }

        [TestMethod]
        public void PointsAreClampedToTheConfiguredRange()
        {
            _personal.AddPersonal("npc-1", 99999, "Deed_Test");

            Assert.AreEqual(1000, _personal.GetPersonal("npc-1"));
        }

        [TestMethod]
        public void RescuedNpcOfAHostileFactionStopsAttacking()
        {
            _factions.SetPlayerRelation(Fractions.Dwarf, RelationLevel.Hostility);
            Assert.IsTrue(_personal.IsHostileToPlayer("dwarf-1", Fractions.Dwarf));

            _personal.AddPersonal("dwarf-1", 300, "Deed_Test"); // +1 step: Hostility → Dislike

            Assert.AreEqual(RelationLevel.Dislike, _personal.GetEffectiveRelation("dwarf-1", Fractions.Dwarf));
            Assert.IsFalse(_personal.IsHostileToPlayer("dwarf-1", Fractions.Dwarf));
            Assert.IsTrue(_personal.IsHostileToPlayer("dwarf-2", Fractions.Dwarf)); // the rest of the faction still attacks
        }

        [TestMethod]
        public void EffectiveRelationIsClampedToTheLadderEnds()
        {
            _personal.SetPersonal("undead-1", 1000); // +2 from Hostility would pass Neutral — fine
            Assert.AreEqual(RelationLevel.Neutral, _personal.GetEffectiveRelation("undead-1", Fractions.Undead));

            _factions.SetPlayerRelation(Fractions.Elf, RelationLevel.Alliance);
            _personal.SetPersonal("elf-1", 1000);
            Assert.AreEqual(RelationLevel.Alliance, _personal.GetEffectiveRelation("elf-1", Fractions.Elf)); // no overflow past the top
        }

        [TestMethod]
        public void FinalDeathAndRisingEraseTheMemory()
        {
            _personal.SetPersonal("npc-1", 500);
            _personal.SetPersonal("npc-2", 500);

            _bus.Publish(new NpcFinalDeathEvent("npc-1", "skeleton", Vector2.Zero));
            _bus.Publish(new NpcFactionChangedEvent("npc-2", "villager", Fractions.Human, Fractions.Undead, Vector2.Zero));

            Assert.AreEqual(0, _personal.GetPersonal("npc-1"));
            Assert.AreEqual(0, _personal.GetPersonal("npc-2"));
        }

        [TestMethod]
        public void SaveParticipantRoundTripsThePoints()
        {
            _personal.SetPersonal("npc-1", 450);
            _personal.SetPersonal("npc-2", -600);
            var captured = new PersonalReputationSaveParticipant(_personal).Capture();

            var target = new PersonalReputationService(_factions, new GameEventBus());
            new PersonalReputationSaveParticipant(target).Restore(captured, 1);

            Assert.AreEqual(450, target.GetPersonal("npc-1"));
            Assert.AreEqual(-600, target.GetPersonal("npc-2"));
        }
    }
}
