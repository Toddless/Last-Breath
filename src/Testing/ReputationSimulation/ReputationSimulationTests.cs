namespace LastBreathTest.ReputationSimulation
{
    using Core.Ai.World;
    using Core.Ai.World.Raids;
    using Core.Components;
    using Core.Data.GameData;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;
    using Core.Reputation;
    using Core.Services;
    using Godot;
    using Moq;
    using Newtonsoft.Json;

    /// <summary>
    /// Balance invariants over the REAL SharedData configs — the reputation counterpart of the
    /// loot simulation's fast checks. If a data tweak breaks one of these, the game feel it
    /// protects (anti-farm ceilings, the death-spiral floor, no threshold flicker, raids that
    /// actually arrive) broke with it.
    /// </summary>
    [TestClass]
    public class ReputationSimulationTests
    {
        private ReputationPipeline _pipeline = null!;

        [TestInitialize]
        public void Setup() => _pipeline = ReputationPipeline.Create();

        [TestMethod]
        public void KillFarmingCapsAtHostility_HatredNeedsHeavierDeedsThanMurder()
        {
            var levelLog = new List<RelationLevel>();
            _pipeline.Relations.PlayerRelationChanged += (faction, level) =>
            {
                if (faction == Fractions.Elf) levelLog.Add(level);
            };

            int killsToHostility = -1;
            for (int i = 0; i < 1000; i++)
            {
                _pipeline.KillNpc(Fractions.Elf);
                if (killsToHostility < 0 && _pipeline.Relations.GetPlayerRelation(Fractions.Elf) == RelationLevel.Hostility)
                    killsToHostility = i + 1;
            }

            // The no-penalty floor is the murder ceiling: on-sight hostility, never Hatred (raids
            // are reserved for heavier future deeds — quest betrayals, massacres of witnesses).
            Assert.AreEqual(RelationLevel.Hostility, _pipeline.Relations.GetPlayerRelation(Fractions.Elf));
            Assert.IsFalse(levelLog.Contains(RelationLevel.Hatred), "kills alone must never reach Hatred");
            Assert.IsTrue(killsToHostility is > 3 and <= 20,
                $"a faction turned hostile after {killsToHostility} kills — aggression must have consequences, but not from one slip");

            // Once they attack on sight, self-defense is free: the points are frozen.
            int frozen = _pipeline.Relations.GetReputation(Fractions.Elf);
            _pipeline.KillNpc(Fractions.Elf);
            Assert.AreEqual(frozen, _pipeline.Relations.GetReputation(Fractions.Elf));
        }

        [TestMethod]
        public void UndeadFarmingNeverLiftsALivingFactionALevel()
        {
            var livingFactions = new[] { Fractions.Human, Fractions.Elf, Fractions.Dwarf };
            var levelsBefore = livingFactions.ToDictionary(faction => faction, _pipeline.Relations.GetPlayerRelation);
            var pointsBefore = livingFactions.ToDictionary(faction => faction, _pipeline.Relations.GetReputation);
            int undeadBefore = _pipeline.Relations.GetReputation(Fractions.Undead);

            for (int i = 0; i < 1000; i++)
                _pipeline.KillNpc(Fractions.Undead);

            // Undead start hostile — killing them is free for their own standing.
            Assert.AreEqual(undeadBefore, _pipeline.Relations.GetReputation(Fractions.Undead));

            foreach (var faction in livingFactions)
            {
                // The repeat decay caps the gratitude: an undead-grinding session earns goodwill,
                // never a standing level — levels are for deliberate deeds.
                Assert.AreEqual(levelsBefore[faction], _pipeline.Relations.GetPlayerRelation(faction),
                    $"{faction} gained a standing level from undead farming");
                int earned = _pipeline.Relations.GetReputation(faction) - pointsBefore[faction];
                Assert.IsTrue(earned is > 0 and <= 200, $"{faction} earned {earned} from undead farming");
            }
        }

        [TestMethod]
        public void UnwitnessedMassMurderChangesNothing()
        {
            _pipeline.Witnesses.Witnessed = false;

            for (int i = 0; i < 1000; i++)
            {
                _pipeline.KillNpc(Fractions.Elf);
                _pipeline.KillNpc(Fractions.Undead);
            }

            foreach (Fractions faction in Enum.GetValues<Fractions>())
            {
                int expected = _pipeline.FactionData.PlayerDefaults
                    .Where(entry => entry.Fraction == faction.ToString())
                    .Select(entry => entry.Points)
                    .FirstOrDefault();
                Assert.AreEqual(expected, _pipeline.Relations.GetReputation(faction), $"{faction} moved without witnesses");
            }
        }

        [TestMethod]
        public void HysteresisNeverFlickersAroundAnyThreshold()
        {
            var scale = _pipeline.FactionData.Reputation;
            int wobble = scale.Hysteresis - 1;

            foreach (var entry in scale.Levels.Where(level => level.From > scale.Min))
            {
                // Settle just above the boundary, then wobble by less than the buffer.
                _pipeline.Relations.SetReputation(Fractions.Elf, entry.From + scale.Hysteresis + 1);

                int levelChanges = 0;
                _pipeline.Relations.PlayerRelationChanged += Count;
                for (int i = 0; i < 200; i++)
                {
                    _pipeline.Relations.AddReputation(Fractions.Elf, -wobble, "Sim");
                    _pipeline.Relations.AddReputation(Fractions.Elf, wobble, "Sim");
                }

                _pipeline.Relations.PlayerRelationChanged -= Count;
                Assert.AreEqual(0, levelChanges, $"the level flickered around '{entry.Level}' ({entry.From})");
                continue;

                void Count(Fractions faction, RelationLevel level) => levelChanges++;
            }
        }

        [TestMethod]
        public void RealConfigsAreCoherent()
        {
            var scale = _pipeline.FactionData.Reputation;
            var thresholds = scale.Levels.Select(entry => entry.From).ToList();

            // Every ladder level has a threshold, strictly ascending, anchored at the scale bottom.
            Assert.AreEqual(Enum.GetValues<RelationLevel>().Length, scale.Levels.Count);
            CollectionAssert.AreEqual(thresholds.OrderBy(from => from).ToList(), thresholds);
            Assert.AreEqual(scale.Min, thresholds[0]);
            Assert.IsTrue(thresholds[^1] < scale.Max);

            // The buffer must be smaller than half the narrowest band, or a band becomes unreachable.
            int narrowestBand = thresholds.Zip(thresholds.Skip(1), (a, b) => b - a).Min();
            Assert.IsTrue(scale.Hysteresis > 0 && scale.Hysteresis * 2 < narrowestBand);

            // Personal layer: the top shift must be earnable within the points range.
            var personal = _pipeline.FactionData.PersonalReputation;
            Assert.IsTrue(personal.PointsPerShift > 0 && personal.MaxShift >= 1);
            Assert.IsTrue(personal.PointsPerShift * personal.MaxShift <= personal.Max);

            // Raiding factions must carry reputation; undead must start hostile (core gameplay).
            foreach (var traits in _pipeline.FactionData.Factions.Where(entry => entry.CanRaid))
                Assert.IsTrue(traits.HasReputation, $"{traits.Fraction} can raid but has no reputation");
            Assert.IsTrue(_pipeline.Relations.IsHostileToPlayer(Fractions.Undead));

            // The kill deed: negative, witnessed, floored, with sane anti-farm decay.
            var kill = _pipeline.DeedsData.Deeds.Single(deed => deed.Id == DeedIds.KillNpc);
            Assert.IsTrue(kill.Reputation < 0);
            Assert.IsTrue(kill.RequiresWitness);
            Assert.IsNotNull(kill.NoPenaltyAtOrBelow);
            Assert.IsTrue(kill is { RepeatDecay: > 0 and <= 1 });
            Assert.IsTrue(kill.HostileToTargetBonus >= 0);
            Assert.IsTrue(_pipeline.DeedsData.WitnessRadius > 0);

            // Raids: probabilities and squad sizes that can actually fire.
            var raids = _pipeline.RaidsData;
            Assert.IsTrue(raids is { Chance: > 0 and <= 1 });
            Assert.IsTrue(raids.SquadSizeMin >= 1 && raids.SquadSizeMin <= raids.SquadSizeMax);
            Assert.IsTrue(raids.CheckIntervalSeconds > 0 && raids.DurationSeconds > 0 && raids.CooldownSeconds > 0);
            Assert.IsTrue(raids.MoveSpeed > 0);

            // Perks: magnitudes of price/reward changes stay within ±100%.
            foreach (var level in Enum.GetValues<RelationLevel>())
                foreach (var perk in _pipeline.Perks.GetPerksForLevel(level))
                    if (perk.Id is "Perk_Price_Change" or "Perk_Reward_Change")
                        Assert.IsTrue(Math.Abs(perk.Value) <= 1f, $"{perk.Id}={perk.Value} at {level}");
        }

        [TestMethod]
        public void RaidsArriveReliablyAtHatred_SeededStatistics()
        {
            var raids = _pipeline.RaidsData;
            var arrivals = new List<float>();

            for (int seed = 0; seed < 200; seed++)
                arrivals.Add(RunRaidSession(seed));

            float worstCase = raids.InitialDelaySeconds + raids.CheckIntervalSeconds * 60; // P(miss 60 checks) ≈ 0.65^60
            Assert.IsTrue(arrivals.All(time => time >= raids.InitialDelaySeconds), "a raid fired inside the grace period");
            Assert.IsTrue(arrivals.All(time => time <= worstCase), "a session never got raided at Hatred");

            var median = arrivals.OrderBy(time => time).ElementAt(arrivals.Count / 2);
            Assert.IsTrue(median <= raids.InitialDelaySeconds + raids.CheckIntervalSeconds * 10,
                $"median time to the first raid is {median}s — Hatred should feel dangerous");
        }

        /// <summary>One seeded session: real service + real config, ticked at 1s; returns the first raid time.</summary>
        private float RunRaidSession(int seed)
        {
            var bus = new GameEventBus();
            var relations = new FactionRelationService(_pipeline.FactionData);
            relations.SetPlayerRelation(Fractions.Elf, RelationLevel.Hatred);

            var sites = new RaidSpawnRegistry();
            sites.Register(new SimSite(Fractions.Elf, new Vector2(500, 0), "elf_warrior"));

            var player = new Mock<IPlayer>();
            player.SetupGet(p => p.IsAlive).Returns(true);
            player.SetupGet(p => p.IsFighting).Returns(false);
            var accessor = new Mock<IPlayerAccessor>();
            accessor.SetupGet(a => a.Player).Returns(player.Object);

            var provider = new Mock<INpcProvider>();
            provider.Setup(p => p.CreateDefinition(It.IsAny<string>())).Returns<string>(npcId => new NpcDefinition
            {
                NpcId = npcId,
                Fraction = Fractions.Elf,
                Parameters = new Dictionary<EntityParameter, float>(),
                Abilities = [],
                Lifecycle = new NpcLifecycleConfig(),
            });

            var messages = new Mock<Core.MessageBus.IGameMessageBus>();
            messages.Setup(m => m.PublishMessageAsync(It.IsAny<Core.Events.SendNotificationMessageMessage>())).Returns(Task.CompletedTask);

            var service = new SimRaidService(relations, sites, provider.Object, new SimSpawner(), new NpcPopulationService(bus),
                accessor.Object, bus, messages.Object, new DefaultRandomNumberGenerator(seed));
            service.Apply(DataCatalog.Raids, new GameDataFile("Raids.json", JsonConvert.SerializeObject(_pipeline.RaidsData)));

            float started = -1f;
            bus.Subscribe<RaidStartedEvent>(_ => started = 0f);

            float time = 0f;
            while (started < 0 && time < 7200f)
            {
                service.Tick(1f);
                time += 1f;
                if (started == 0f) started = time;
            }

            Assert.IsTrue(started > 0, $"seed {seed}: no raid within two hours");
            return started;
        }

        private sealed class SimRaidService(
            IFactionRelationService relations, IRaidSpawnRegistry sites, INpcProvider provider, INpcWorldSpawner spawner,
            INpcPopulationService population, IPlayerAccessor accessor, GameEventBus bus, Core.MessageBus.IGameMessageBus messages, IRandomNumberGenerator rnd)
            : RaidService(relations, sites, provider, spawner, population, accessor, bus, messages, rnd)
        {
            protected override Vector2? GetPlayerPosition() => Vector2.Zero;
        }

        private sealed class SimSite(Fractions fraction, Vector2 position, params string[] ids) : IRaidSpawnSite
        {
            public Fractions? Fraction => fraction;
            public Vector2 Position => position;
            public IReadOnlyList<string> NpcIds => ids;
        }

        private sealed class SimSpawner : INpcWorldSpawner
        {
            public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position)
            {
                var npc = new Mock<IFightableNpc>();
                npc.SetupGet(n => n.IsAlive).Returns(true);
                npc.SetupGet(n => n.InstanceId).Returns(Guid.NewGuid().ToString());
                npc.SetupGet(n => n.Position).Returns(position);
                npc.As<IWorldAgent>();
                return npc.Object;
            }

            public void Despawn(IFightableNpc npc)
            {
            }
        }
    }
}
