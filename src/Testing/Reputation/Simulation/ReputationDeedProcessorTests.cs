namespace LastBreathTest.Reputation.Simulation
{
    using Core.Data.GameData;
    using Core.Data.ReputationData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Reputation;
    using Core.Save;
    using Core.Services;
    using Godot;
    using Moq;
    using Newtonsoft.Json;

    [TestClass]
    public class ReputationDeedProcessorTests
    {
        private GameEventBus _bus = null!;
        private FactionRelationService _relations = null!;
        private LoadScope _loadScope = null!;
        private IPlayer _player = null!;
        private FakeWitnessQuery _witnesses = null!;
        private PersonalReputationService _personal = null!;

        [TestInitialize]
        public void Setup()
        {
            _bus = new GameEventBus();
            _relations = new FactionRelationService(FactionTestData.Create());
            _loadScope = new LoadScope();
            _player = new Mock<IPlayer>().Object;
            _witnesses = new FakeWitnessQuery();
            _personal = new PersonalReputationService(_relations, _bus);

            var accessor = new Mock<IPlayerAccessor>();
            accessor.SetupGet(a => a.Player).Returns(() => _player);

            var processor = new ReputationDeedProcessor(_bus, _relations, accessor.Object, _loadScope, _witnesses, _personal);
            processor.Apply(DataCatalog.ReputationDeeds, DeedsFile());
        }

        [TestMethod]
        public void PlayerKillCostsReputationAndPleasesTheVictimsEnemies()
        {
            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf), _player));

            Assert.AreEqual(-120, _relations.GetReputation(Fractions.Elf));
            // Undead → Elf is Hostility in the matrix: they approve. Dwarves only dislike elves — no bonus.
            Assert.AreEqual(-1000 + 10, _relations.GetReputation(Fractions.Undead));
            Assert.AreEqual(0, _relations.GetReputation(Fractions.Dwarf));
        }

        [TestMethod]
        public void KillingAnAlreadyHostileFactionIsFreeButItsEnemiesStillApprove()
        {
            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Undead), _player));

            Assert.AreEqual(-1000, _relations.GetReputation(Fractions.Undead)); // floor: standing is already Hostility
            Assert.AreEqual(10, _relations.GetReputation(Fractions.Elf));
            Assert.AreEqual(10, _relations.GetReputation(Fractions.Dwarf));
            Assert.AreEqual(510, _relations.GetReputation(Fractions.Human));
        }

        [TestMethod]
        public void DeathsWithoutThePlayerAsKillerAreIgnored()
        {
            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf))); // skirmish/lifecycle death — no killer
            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf), Npc(Fractions.Undead))); // NPC killed NPC

            Assert.AreEqual(0, _relations.GetReputation(Fractions.Elf));
        }

        [TestMethod]
        public void DeathsDuringSaveLoadAreIgnored()
        {
            using (_loadScope.Begin())
                _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf), _player));

            Assert.AreEqual(0, _relations.GetReputation(Fractions.Elf));
        }

        [TestMethod]
        public void RepeatedKillsDecay()
        {
            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf), _player));
            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf), _player));

            Assert.AreEqual(-120 - 108, _relations.GetReputation(Fractions.Elf)); // −120, then −120 × 0.9
        }

        [TestMethod]
        public void ExplicitDeedEventAppliesFactionAndPersonalDeltas()
        {
            _bus.Publish(new PlayerDeedEvent("Deed_Test_Donation", Fractions.Dwarf, "dwarf-1", Vector2.Zero));

            Assert.AreEqual(100, _relations.GetReputation(Fractions.Dwarf));
            Assert.AreEqual(50, _personal.GetPersonal("dwarf-1"));
        }

        [TestMethod]
        public void PersonalDeltaNeedsATargetInstance()
        {
            _bus.Publish(new PlayerDeedEvent("Deed_Test_Donation", Fractions.Dwarf, null, Vector2.Zero));

            Assert.AreEqual(100, _relations.GetReputation(Fractions.Dwarf));
            Assert.AreEqual(0, _personal.Snapshot.Count);
        }

        [TestMethod]
        public void UnwitnessedKillIsSilentForEveryFaction()
        {
            _witnesses.Result = false;

            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf), _player));

            Assert.AreEqual(0, _relations.GetReputation(Fractions.Elf));
            Assert.AreEqual(-1000, _relations.GetReputation(Fractions.Undead)); // no approval bonus either

            // The next witnessed kill is still the first "known" one — no decay burned on the silent kill.
            _witnesses.Result = true;
            _bus.Publish(new EntityDiedEvent(Npc(Fractions.Elf), _player));
            Assert.AreEqual(-120, _relations.GetReputation(Fractions.Elf));
        }

        [TestMethod]
        public void UnknownDeedChangesNothing()
        {
            _bus.Publish(new PlayerDeedEvent("Deed_Not_In_Catalog", Fractions.Dwarf, null, Vector2.Zero));

            Assert.AreEqual(0, _relations.GetReputation(Fractions.Dwarf));
        }

        /// <summary>Mirrors SharedData/ReputationDeeds/ReputationDeeds.json plus a test-only donation deed.</summary>
        private static GameDataFile DeedsFile() => new("ReputationDeeds.json", JsonConvert.SerializeObject(new ReputationDeedsData
        {
            Deeds =
            [
                new ReputationDeedEntry
                {
                    Id = DeedIds.KillNpc, Reputation = -120, NoPenaltyAtOrBelow = "Hostility",
                    HostileToTargetBonus = 10, RequiresWitness = true, RepeatDecay = 0.9f,
                },
                new ReputationDeedEntry { Id = "Deed_Test_Donation", Reputation = 100, Personal = 50 },
            ],
        }));

        private static IFightableNpc Npc(Fractions fraction)
        {
            var npc = new Mock<IFightableNpc>();
            npc.SetupGet(n => n.Fraction).Returns(fraction);
            npc.SetupGet(n => n.InstanceId).Returns("npc-test");
            npc.SetupGet(n => n.Position).Returns(Vector2.Zero);
            return npc.Object;
        }

        private sealed class FakeWitnessQuery : IWitnessQuery
        {
            public bool Result { get; set; } = true;

            public bool HasWitness(Vector2 position, float radius, string? excludeInstanceId = null) => Result;
        }
    }
}
