namespace LastBreathTest.WorldTesting
{
    using BattleSystemTests;
    using Core.Ai.World;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Data.NpcData;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Reputation;
    using Core.Save.Participants;
    using Core.Services;
    using Godot;
    using Moq;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class WorldSaveParticipantsTests
    {
        private const string VillagerNpcId = "villager_smith";
        private const string RaiderNpcId = "undead_raider";
        private const float RecoverMinSeconds = 40f;
        private const float RecoverMaxSeconds = 80f;

        /// <summary>What <see cref="FixedRandom"/> rolls out of the recovery range: its midpoint.</summary>
        private const float RolledRecoverySeconds = (RecoverMinSeconds + RecoverMaxSeconds) / 2f;

        /// <summary>The delay a record from an older save carries: inside the recovery range but NOT its
        /// midpoint, so restoring it cannot be confused with a fresh <see cref="FixedRandom"/> roll.</summary>
        private const float SavedDelaySeconds = 70f;

        private const float LainSeconds = 25f;
        private const float LastSecond = 1f;
        private const float LegacyRisingBonus = 0.4f;
        private const float NoRisingBonus = 0f;
        private const float BodyX = 120f;
        private const float BodyY = -45f;
        private const float Tolerance = 0.001f;

        [TestMethod]
        public void WorldClockRoundTripsDayAndTime()
        {
            var source = new WorldClock(new WorldClockConfig { StartHour = 8 });
            source.Tick(60f * 30); // 24 real min/day => +30 game hours: day 2, 14:00
            var captured = new WorldClockSaveParticipant(source).Capture();

            var target = new WorldClock(new WorldClockConfig { StartHour = 8 });
            new WorldClockSaveParticipant(target).Restore(captured, 1);

            Assert.AreEqual(source.Day, target.Day);
            Assert.AreEqual(source.Hour, target.Hour);
            Assert.AreEqual(source.Minute, target.Minute);
            Assert.AreEqual(source.Phase, target.Phase);
        }

        [TestMethod]
        public void WorldClockRestoreFiresPhaseChangeForConsumers()
        {
            var clock = new WorldClock(new WorldClockConfig { StartHour = 12 }); // Day phase
            DayPhase? seen = null;
            clock.PhaseChanged += phase => seen = phase;

            clock.RestoreState(day: 3, minuteOfDay: 23 * 60); // 23:00 = Night

            Assert.AreEqual(DayPhase.Night, seen); // tint/schedules react without an extra tick
        }

        [TestMethod]
        public void FactionRelationsRoundTripPointsAndHysteresisState()
        {
            var source = new FactionRelationService(FactionTestData.Create());
            source.AddReputation(Fractions.Elf, 1100, "Deed"); // Friendly
            source.AddReputation(Fractions.Elf, -120, "Deed"); // 980 — Friendly held only by hysteresis
            source.SetPlayerRelation(Fractions.Dwarf, RelationLevel.Dislike);
            var captured = new FactionRelationsSaveParticipant(source).Capture();

            var target = new FactionRelationService(FactionTestData.Create());
            new FactionRelationsSaveParticipant(target).Restore(captured, 2);

            Assert.AreEqual(980, target.GetReputation(Fractions.Elf));
            Assert.AreEqual(RelationLevel.Friendly, target.GetPlayerRelation(Fractions.Elf)); // stateless resolve would say Neutral
            Assert.AreEqual(RelationLevel.Dislike, target.GetPlayerRelation(Fractions.Dwarf));
            Assert.AreEqual(-1000, target.GetReputation(Fractions.Undead)); // untouched default survives
        }

        [TestMethod]
        public void FactionRelationsMigrateV1LevelsToBandMidpoints()
        {
            var target = new FactionRelationService(FactionTestData.Create());
            var legacy = JToken.Parse("""{"playerRelations":{"Undead":"Hatred","Elf":"Dislike","Ghost":"Hatred"}}""");

            new FactionRelationsSaveParticipant(target).Restore(legacy, 1);

            Assert.AreEqual(RelationLevel.Hatred, target.GetPlayerRelation(Fractions.Undead));
            Assert.AreEqual((-6000 + -2000) / 2, target.GetReputation(Fractions.Undead));
            Assert.AreEqual(RelationLevel.Dislike, target.GetPlayerRelation(Fractions.Elf));
            Assert.AreEqual((-800 + -200) / 2, target.GetReputation(Fractions.Elf));
        }

        [TestMethod]
        public void LifecycleRestoreKeepsRemainingRiseTime()
        {
            var lifecycle = new NpcLifecycle(new NpcLifecycleConfig
            {
                ResurrectMinSeconds = 60f,
                ResurrectMaxSeconds = 600f,
                MaxStrengthBonus = 1f
            }, new FixedRandom());

            float? risingBonus = null;
            lifecycle.ResurrectionReady += bonus => risingBonus = bonus;

            lifecycle.RestoreState(NpcLifeStage.Defeated, resurrectDelay: 330f, elapsed: 300f);

            lifecycle.Tick(29f);
            Assert.AreEqual(NpcLifeStage.Defeated, lifecycle.Stage); // 29 of the remaining 30 seconds
            lifecycle.Tick(2f);
            Assert.IsNotNull(risingBonus);
            Assert.AreEqual(0.5f, risingBonus.Value, 0.001f); // 330 is halfway between 60 and 600
        }

        [TestMethod]
        public void LifecycleRestoreDormantWaitsForBurning()
        {
            var lifecycle = new NpcLifecycle(new NpcLifecycleConfig(), new FixedRandom());

            lifecycle.RestoreState(NpcLifeStage.Dormant, 0f, 0f);
            lifecycle.Tick(100000f);

            Assert.AreEqual(NpcLifeStage.Dormant, lifecycle.Stage);
            Assert.IsTrue(lifecycle.CanBeBurned);
            Assert.IsTrue(lifecycle.TryBurn());
        }

        /// <summary>
        /// The whole save path of a peaceful resident's body: it is written as a lying body, the load
        /// builds the cycle its definition asks for, the timer resumes where it stopped and the
        /// resident gets back up ALIVE — no undead turn anywhere along the way.
        /// </summary>
        [TestMethod]
        public void LyingVillagerRoundTripsAsABodyAndGetsBackUpAlive()
        {
            var definition = VillagerDefinition();
            var source = new TestNpc(definition, new Vector2(BodyX, BodyY));
            source.LayDown();
            source.Lifecycle.Tick(LainSeconds);

            Assert.IsFalse(source.IsAlive, "precondition: the villager is really down");
            Assert.AreEqual(NpcLifeStage.Defeated, source.Lifecycle.Stage, "precondition: the body really lies");
            Assert.IsInstanceOfType<IAliveRiseLifecycle>(source.Lifecycle, "precondition: the villager cycle, not the undead one");

            var registry = new NpcWorldRegistry();
            registry.Register(source.Participant);
            var spawner = new FakeSpawner();
            var participant = Participant(registry, spawner, definition);

            var captured = participant.Capture();

            var body = Bodies(captured).Single();
            Assert.AreEqual(NpcBodySaveData.DefeatedKind, body.Kind);
            Assert.AreEqual(RolledRecoverySeconds, body.ResurrectDelay, Tolerance);
            Assert.AreEqual(LainSeconds, body.Elapsed, Tolerance);

            participant.Restore(captured, participant.Version);

            var restored = spawner.Spawned.Single();
            Assert.IsInstanceOfType<IAliveRiseLifecycle>(restored.Lifecycle, "the definition's kind builds the cycle on load too");
            Assert.IsNotInstanceOfType<IUndeadRiseLifecycle>(restored.Lifecycle, "a resident must not come back on the undead cycle");
            Assert.AreEqual(NpcLifeStage.Defeated, restored.Lifecycle.Stage, "the restored villager lies");
            Assert.IsFalse(restored.IsAlive);
            Assert.AreEqual(BodyX, restored.Position.X, Tolerance); // lies where it fell
            Assert.AreEqual(BodyY, restored.Position.Y, Tolerance);
            Assert.AreEqual(RolledRecoverySeconds, restored.Lifecycle.ResurrectDelay, Tolerance);
            Assert.AreEqual(LainSeconds, restored.Lifecycle.Elapsed, Tolerance);

            restored.Lifecycle.Tick(RolledRecoverySeconds - LainSeconds - LastSecond);
            Assert.AreEqual(NpcLifeStage.Defeated, restored.Lifecycle.Stage, "the timer resumed, it did not start over");

            restored.Lifecycle.Tick(LastSecond);
            Assert.IsTrue(restored.Revived, "the resident stood up on their own feet");
            Assert.IsTrue(restored.IsAlive);
            Assert.IsFalse(restored.IsRisen);
            Assert.AreEqual(NpcLifeStage.Alive, restored.Lifecycle.Stage);
        }

        /// <summary>
        /// A save written while this NPC still rose undead: today the record names a resident, and the
        /// cycle the definition builds must win over the fate written in the file.
        /// </summary>
        [TestMethod]
        public void RisenRecordCannotRaiseAVillagerAsUndead()
        {
            var definition = VillagerDefinition();
            var spawner = new FakeSpawner();
            var participant = Participant(new NpcWorldRegistry(), spawner, definition);
            var legacy = JToken.FromObject(new NpcWorldSaveData
            {
                Bodies = [Record(NpcBodySaveData.RisenKind, definition.NpcId, LegacyRisingBonus)]
            });

            participant.Restore(legacy, participant.Version);

            var restored = spawner.Spawned.Single(); // the resident is in the world, not dropped
            Assert.IsFalse(restored.IsRisen, "the record's kind must not turn today's resident into undead");
            Assert.AreNotEqual(Fractions.Undead, restored.Fraction);
            Assert.AreEqual(0f, restored.RisingBonus, Tolerance);
            Assert.IsTrue(restored.IsAlive, "a resident who was up and about is simply alive");
            Assert.AreEqual(NpcLifeStage.Alive, restored.Lifecycle.Stage);
        }

        /// <summary>
        /// The same old save, the other way a body could be written: dormant is a stage the resident
        /// cycle does not count down, so the body lies as defeated and recovers instead of waiting forever.
        /// </summary>
        [TestMethod]
        public void DormantRecordLaysAVillagerDownAndLetsThemRecover()
        {
            var definition = VillagerDefinition();
            var spawner = new FakeSpawner();
            var participant = Participant(new NpcWorldRegistry(), spawner, definition);
            var legacy = JToken.FromObject(new NpcWorldSaveData
            {
                Bodies = [Record(NpcBodySaveData.DormantKind, definition.NpcId)]
            });

            participant.Restore(legacy, participant.Version);

            var restored = spawner.Spawned.Single();
            Assert.AreEqual(NpcLifeStage.Defeated, restored.Lifecycle.Stage, "a resident has one lying stage, and it counts down");
            Assert.IsFalse(restored.IsAlive);
            Assert.AreEqual(SavedDelaySeconds, restored.Lifecycle.ResurrectDelay, Tolerance, "the saved delay, not a fresh roll");
            Assert.AreEqual(LainSeconds, restored.Lifecycle.Elapsed, Tolerance);

            restored.Lifecycle.Tick(SavedDelaySeconds - LainSeconds);
            Assert.IsTrue(restored.Revived);
            Assert.IsFalse(restored.IsRisen, "recovering is not rising");
        }

        /// <summary>An NPC whose cycle does raise undead still comes back risen — the guard above
        /// refuses the ending, not the record.</summary>
        [TestMethod]
        public void RisenRecordStillRestoresAnUndeadRising()
        {
            var definition = UndeadCycleDefinition();
            var spawner = new FakeSpawner();
            var participant = Participant(new NpcWorldRegistry(), spawner, definition);
            var saved = JToken.FromObject(new NpcWorldSaveData
            {
                Bodies = [Record(NpcBodySaveData.RisenKind, definition.NpcId, LegacyRisingBonus)]
            });

            participant.Restore(saved, participant.Version);

            var restored = spawner.Spawned.Single();
            Assert.IsTrue(restored.IsRisen);
            Assert.AreEqual(Fractions.Undead, restored.Fraction);
            Assert.AreEqual(LegacyRisingBonus, restored.RisingBonus, Tolerance);
        }

        /// <summary>Spawn points re-roll the living on load, so only bodies are written: a resident on
        /// their feet — and one already down whose cycle has not laid them out yet — stay out of the file.</summary>
        [TestMethod]
        public void VillagerOnTheirFeetIsNotSavedAsABody()
        {
            var definition = VillagerDefinition();
            var standing = new TestNpc(definition, new Vector2(BodyX, BodyY));
            var falling = new TestNpc(definition, new Vector2(BodyX, BodyY)) { IsAlive = false }; // zeroed, cycle not told yet

            Assert.IsTrue(standing.IsAlive, "precondition: on their feet");
            Assert.AreEqual(NpcLifeStage.Alive, standing.Lifecycle.Stage);
            Assert.AreEqual(NpcLifeStage.Alive, falling.Lifecycle.Stage, "precondition: no lying stage to write yet");

            var registry = new NpcWorldRegistry();
            registry.Register(standing.Participant);
            registry.Register(falling.Participant);

            var captured = Participant(registry, new FakeSpawner(), definition).Capture();

            Assert.AreEqual(0, Bodies(captured).Count);
        }

        private static NpcWorldSaveParticipant Participant(INpcWorldRegistry registry, INpcWorldSpawner spawner, params NpcDefinition[] known)
        {
            var provider = new Mock<INpcProvider>();
            provider.SetupGet(p => p.KnownNpcIds).Returns(known.Select(d => d.NpcId).ToArray());
            provider.Setup(p => p.CreateDefinition(It.IsAny<string>(), It.IsAny<NpcDefinitionOverrides?>()))
                .Returns<string, NpcDefinitionOverrides?>((npcId, _) => known.First(d => d.NpcId == npcId));

            var population = new Mock<INpcPopulationService>();
            population.Setup(p => p.TryReserve()).Returns(true);

            return new NpcWorldSaveParticipant(registry, provider.Object, population.Object, spawner);
        }

        private static List<NpcBodySaveData> Bodies(JToken captured) => captured.ToObject<NpcWorldSaveData>()!.Bodies;

        /// <summary>A body record of an older save: the timer values are the ones a body carries.</summary>
        private static NpcBodySaveData Record(string kind, string npcId, float risingBonus = NoRisingBonus) => new()
        {
            Kind = kind,
            NpcId = npcId,
            X = BodyX,
            Y = BodyY,
            ResurrectDelay = SavedDelaySeconds,
            Elapsed = LainSeconds,
            RisingBonus = risingBonus,
        };

        private static NpcDefinition VillagerDefinition() => new()
        {
            NpcId = VillagerNpcId,
            Fraction = Fractions.Human,
            Parameters = new Dictionary<EntityParameter, float>(),
            Abilities = [],
            Lifecycle = new NpcLifecycleConfig(),
            LifecycleKind = NpcLifecycleKind.Villager,
            VillagerLifecycle = new VillagerLifecycleConfig
            {
                RecoverMinSeconds = RecoverMinSeconds,
                RecoverMaxSeconds = RecoverMaxSeconds,
            },
        };

        private static NpcDefinition UndeadCycleDefinition() => new()
        {
            NpcId = RaiderNpcId,
            Fraction = Fractions.Human,
            Parameters = new Dictionary<EntityParameter, float>(),
            Abilities = [],
            Lifecycle = new NpcLifecycleConfig(),
        };

        /// <summary>
        /// A world NPC as the save participant sees one: the cycle its definition asks for, built by
        /// the same factory the node uses, and the calls wired the way BaseNpc wires them — going down
        /// hands the body to the cycle, a restored body drops to zero health and takes the saved stage,
        /// a restored rising turns the NPC undead, and the cycle's ending puts it back on its feet.
        /// </summary>
        private sealed class TestNpc
        {
            public TestNpc(NpcDefinition definition, Vector2 position)
            {
                Lifecycle = NpcLifecycleFactory.Create(definition, new FixedRandom());
                Fraction = definition.Fraction;
                Position = position;

                if (Lifecycle is IUndeadRiseLifecycle undead) undead.ResurrectionReady += OnResurrectionReady;
                if (Lifecycle is IAliveRiseLifecycle alive) alive.ReviveReady += OnReviveReady;

                var abilityBook = new Mock<IAbilityBookComponent>();
                abilityBook.SetupGet(b => b.CurrentStance).Returns(definition.Stance);

                var mock = new Mock<IFightableNpc>();
                mock.SetupGet(n => n.Id).Returns(definition.NpcId);
                mock.SetupGet(n => n.Level).Returns(definition.Level);
                mock.SetupGet(n => n.Rarity).Returns(definition.Rarity);
                mock.SetupGet(n => n.AbilityBook).Returns(abilityBook.Object);
                mock.SetupGet(n => n.Lifecycle).Returns(() => Lifecycle);
                mock.SetupGet(n => n.Position).Returns(() => Position);
                mock.SetupGet(n => n.IsFighting).Returns(false);
                mock.SetupGet(n => n.IsAlive).Returns(() => IsAlive);
                mock.SetupGet(n => n.IsRisen).Returns(() => IsRisen);
                mock.SetupGet(n => n.RisingBonus).Returns(() => RisingBonus);
                mock.Setup(n => n.RestoreAsBody(It.IsAny<NpcLifeStage>(), It.IsAny<float>(), It.IsAny<float>()))
                    .Callback<NpcLifeStage, float, float>((stage, resurrectDelay, elapsed) =>
                    {
                        IsAlive = false; // BaseNpc: CurrentHealth = 0
                        Lifecycle.RestoreState(stage, resurrectDelay, elapsed);
                    });
                mock.Setup(n => n.RestoreAsRisen(It.IsAny<float>())).Callback<float>(OnResurrectionReady);

                Participant = mock.As<ISkirmishParticipant>().Object;
                Npc = mock.Object;
            }

            public INpcLifecycle Lifecycle { get; }
            public IFightableNpc Npc { get; }
            public ISkirmishParticipant Participant { get; }
            public Vector2 Position { get; }
            public bool IsAlive { get; set; } = true;
            public bool IsRisen { get; private set; }
            public bool Revived { get; private set; }
            public float RisingBonus { get; private set; }
            public Fractions Fraction { get; private set; }

            /// <summary>Mirrors BaseNpc.BecomeBody: health to zero, then the cycle takes the body over.</summary>
            public void LayDown()
            {
                IsAlive = false;
                Lifecycle.OnDefeated(Fraction == Fractions.Undead);
            }

            private void OnResurrectionReady(float parameterBonus)
            {
                IsRisen = true;
                IsAlive = true;
                Fraction = Fractions.Undead;
                RisingBonus = parameterBonus;
            }

            private void OnReviveReady()
            {
                Revived = true;
                IsAlive = true;
            }
        }

        private sealed class FakeSpawner : INpcWorldSpawner
        {
            public List<TestNpc> Spawned { get; } = [];

            /// <summary>The node's own load path: ApplyDefinition builds the cycle from the definition.</summary>
            public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position)
            {
                var npc = new TestNpc(definition, position);
                Spawned.Add(npc);
                return npc.Npc;
            }

            public void Despawn(IFightableNpc npc)
            {
            }
        }

        private sealed class FixedRandom : IRandomNumberGenerator
        {
            public float RandFloat() => 0.5f;
            public float RandFloatRange(float min, float max) => (min + max) / 2f;
            public int RandIntRange(int min, int max) => (min + max) / 2;
            public float RandFloatN(float mean, float deviation) => mean;
            public uint RandInt() => 0;
            public long RandWeighted(float[] weights) => 0;
            public long RandWeighted(ReadOnlySpan<float> weights) => 0;
            public void Randomize()
            {
            }
        }
    }
}
