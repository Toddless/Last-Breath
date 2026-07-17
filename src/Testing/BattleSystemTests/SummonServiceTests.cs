namespace LastBreathTest.BattleSystemTests
{
    using Battle.Source;
    using Core.Battle;
    using Core.Entity;
    using Core.Events;
    using Moq;

    /// <summary>
    /// The summon stack: the per-summoner cap (a recast refills the pack, never stacks past it),
    /// the two-phased death removal (resolve-time notice, cleanup after the presentation gate),
    /// the dissolve-on-battle-end and the usual driver gates (dead summoner, finished battle,
    /// refused spawn, dispose). Pure C# + Moq, no Godot runtime.
    /// </summary>
    [TestClass]
    public class SummonServiceTests
    {
        private const int PackSize = 3;

        [TestMethod]
        public void FirstCast_SpawnsTheFullPack()
        {
            var harness = new Harness();

            harness.RequestSummon();

            harness.VerifySpawns(PackSize);
        }

        [TestMethod]
        public void RecastAtFullPack_SpawnsNothing()
        {
            var harness = new Harness();
            harness.RequestSummon();

            harness.RequestSummon();

            harness.VerifySpawns(PackSize);
        }

        [TestMethod]
        public void Recast_RefillsOnlyTheFallen()
        {
            var harness = new Harness();
            harness.RequestSummon();

            harness.KillSummon(0); // resolve-time death notice — cleanup may not have run yet
            harness.RequestSummon();

            harness.VerifySpawns(PackSize + 1);
        }

        [TestMethod]
        public void CleanupDead_RemovesOnlyTheShownDeaths()
        {
            var harness = new Harness();
            harness.RequestSummon();
            harness.KillSummon(1);

            harness.Service.CleanupDead();

            harness.VerifyRemovals(1);
            Assert.IsTrue(harness.WasRemoved(1), "the dead wolf must be the one removed");
        }

        [TestMethod]
        public void CleanupDead_IsIdempotentForDoubleDeathNotices()
        {
            var harness = new Harness();
            harness.RequestSummon();
            harness.KillSummon(0);
            harness.KillSummon(0); // the death event reaches the battle bus twice (resolve + replay)

            harness.Service.CleanupDead();
            harness.Service.CleanupDead();

            harness.VerifyRemovals(1);
        }

        [TestMethod]
        public void DespawnAll_DissolvesTheWholePack()
        {
            var harness = new Harness();
            harness.RequestSummon();

            harness.Service.DespawnAll();

            harness.VerifyRemovals(PackSize);
        }

        [TestMethod]
        public void DeadSummoner_DoesNotSummon()
        {
            var harness = new Harness();
            harness.SummonerAlive = false;

            harness.RequestSummon();

            harness.VerifySpawns(0);
        }

        [TestMethod]
        public void FinishedBattle_DoesNotSummon()
        {
            var harness = new Harness();
            harness.BattleActive = false;

            harness.RequestSummon();

            harness.VerifySpawns(0);
        }

        [TestMethod]
        public void RefusedSpawn_StopsTheCast()
        {
            var harness = new Harness { RefuseSpawns = true };

            harness.RequestSummon();

            harness.VerifySpawns(1); // one refused attempt, no hammering
        }

        [TestMethod]
        public void Dispose_DetachesFromTheSummoner()
        {
            var harness = new Harness();

            harness.Service.Dispose();
            harness.RequestSummon();

            harness.VerifySpawns(0);
        }

        /// <summary>One summoner wired through a real combat bus; the handler is the arena's seam.</summary>
        private sealed class Harness
        {
            private readonly Mock<IFightable> _summoner = new();
            private readonly Mock<ISummonHandler> _handler = new();
            private readonly CombatEventBus _combatEvents = new();
            private readonly List<Mock<IFightableNpc>> _spawned = [];
            private readonly List<IFightableNpc> _removed = [];

            public SummonService Service { get; }
            public bool BattleActive { get; set; } = true;
            public bool SummonerAlive { get; set; } = true;
            public bool RefuseSpawns { get; init; }

            public Harness()
            {
                _summoner.Setup(s => s.InstanceId).Returns(Guid.NewGuid().ToString());
                _summoner.Setup(s => s.CombatEvents).Returns(_combatEvents);
                _summoner.Setup(s => s.IsAlive).Returns(() => SummonerAlive);
                _summoner.Setup(s => s.IsSame(It.IsAny<string>())).Returns((string id) => id == _summoner.Object.InstanceId);

                _handler.Setup(h => h.SpawnSummon(It.IsAny<IFightable>(), It.IsAny<string>(), It.IsAny<float>()))
                    .Returns(() => RefuseSpawns ? null : SpawnWolf());
                _handler.Setup(h => h.RemoveSummon(It.IsAny<IFightableNpc>()))
                    .Callback((IFightableNpc npc) => _removed.Add(npc));

                Service = new SummonService(_handler.Object, () => BattleActive);
                Service.TryAttach(_summoner.Object);
            }

            public void RequestSummon() =>
                _combatEvents.Publish(new SummonRequestedEvent(_summoner.Object, "Npc_Bone_Wolf", PackSize, PackSize, 0.25f));

            public void KillSummon(int index)
            {
                _spawned[index].Setup(npc => npc.IsAlive).Returns(false);
                Service.OnSummonDied(_spawned[index].Object);
            }

            public bool WasRemoved(int index) => _removed.Contains(_spawned[index].Object);

            public void VerifySpawns(int count) =>
                _handler.Verify(h => h.SpawnSummon(It.IsAny<IFightable>(), It.IsAny<string>(), It.IsAny<float>()), Times.Exactly(count));

            public void VerifyRemovals(int count) =>
                _handler.Verify(h => h.RemoveSummon(It.IsAny<IFightableNpc>()), Times.Exactly(count));

            private IFightableNpc SpawnWolf()
            {
                var wolf = new Mock<IFightableNpc>();
                string id = Guid.NewGuid().ToString();
                wolf.Setup(npc => npc.InstanceId).Returns(id);
                wolf.Setup(npc => npc.IsAlive).Returns(true);
                wolf.Setup(npc => npc.IsSummon).Returns(true);
                wolf.Setup(npc => npc.IsSame(It.IsAny<string>())).Returns((string other) => other == id);
                _spawned.Add(wolf);
                return wolf.Object;
            }
        }
    }
}
