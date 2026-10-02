namespace LastBreathTest.Ai
{
    using Core.Ai.World;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.Time;
    using Core.Data;
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
    using Reputation;

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

        /// <summary>A quest's trial target: put into the world by the spawn action, owned by no point.</summary>
        private const string TrialNpcId = "trial_target";

        private const int TrialLevel = 24;
        private const Rarity TrialRarity = Rarity.Epic;
        private const Stance TrialStance = Stance.Strength;
        private const string FirstModifierId = "npc_modifier_frenzied";
        private const string SecondModifierId = "npc_modifier_armored";

        /// <summary>A modifier id a record still names after the catalog dropped it.</summary>
        private const string LostModifierId = "npc_modifier_withdrawn";

        /// <summary>The section version that knew bodies only.</summary>
        private const int LegacySectionVersion = 1;

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

        /// <summary>
        /// A living NPC a quest put into the world: no spawn point re-rolls it, so the save file is
        /// the only way it survives a load — with its identity, its EXACT modifier set and its place.
        /// </summary>
        [TestMethod]
        public void WildLivingNpcRoundTripsWithIdentityModifiersAndPlace()
        {
            var definition = TrialTargetDefinition();
            var target = new TestNpc(definition, new Vector2(BodyX, BodyY)) { IsWild = true };

            Assert.IsTrue(target.IsAlive, "precondition: the trial target stands in the world");
            Assert.IsTrue(target.Npc.IsWild, "precondition: no spawn point owns it");
            Assert.AreEqual(NpcLifeStage.Alive, target.Lifecycle.Stage, "precondition: nothing lies here");
            Assert.AreEqual(2, target.Npc.NpcModifiers.AllModifiers.Count, "precondition: the trial's modifiers are on");

            var registry = new NpcWorldRegistry();
            registry.Register(target.Participant);
            var spawner = new FakeSpawner();
            var population = new Mock<INpcPopulationService>();
            var participant = Participant(registry, spawner, population, definition);

            var captured = participant.Capture();

            var record = Bodies(captured).Single();
            Assert.AreEqual(NpcBodySaveData.AliveKind, record.Kind);
            Assert.AreEqual(TrialNpcId, record.NpcId);
            Assert.AreEqual(TrialLevel, record.Level);
            Assert.AreEqual(TrialRarity, record.Rarity);
            Assert.AreEqual(TrialStance, record.Stance);
            Assert.AreEqual(BodyX, record.X, Tolerance);
            Assert.AreEqual(BodyY, record.Y, Tolerance);
            CollectionAssert.AreEquivalent(new[] { FirstModifierId, SecondModifierId }, record.Modifiers,
                "the trial's own modifiers, not a fresh roll");

            participant.Restore(captured, participant.Version);

            var restored = spawner.Spawned.Single();
            Assert.IsTrue(restored.IsAlive, "the trial target is back on its feet, not lying");
            Assert.AreEqual(NpcLifeStage.Alive, restored.Lifecycle.Stage);
            Assert.AreEqual(TrialNpcId, restored.Definition.NpcId);
            Assert.AreEqual(TrialLevel, restored.Definition.Level);
            Assert.AreEqual(TrialRarity, restored.Definition.Rarity);
            Assert.AreEqual(TrialStance, restored.Definition.Stance);
            CollectionAssert.AreEquivalent(new[] { FirstModifierId, SecondModifierId },
                restored.Definition.Modifiers.Select(modifier => modifier.Id).ToArray(),
                "the same trial, modifier for modifier");
            Assert.AreEqual(BodyX, restored.Position.X, Tolerance);
            Assert.AreEqual(BodyY, restored.Position.Y, Tolerance);
            Assert.IsTrue(restored.Npc.IsWild, "still nobody's: the next save has to carry it too");
            population.Verify(p => p.ReserveOutsideLimit(), Times.Once,
                "a named target takes its slot outside the limit exactly as its first spawn did");
            population.Verify(p => p.TryReserve(), Times.Never, "a full world must never drop it");
        }

        /// <summary>The discrimination itself: a point's own resident is re-rolled by the point on load
        /// and must stay out of the file, while the NPC standing beside it that nobody owns goes in.</summary>
        [TestMethod]
        public void ASpawnPointsLivingNpcStaysOutWhileTheWildOneIsWritten()
        {
            var owned = new TestNpc(VillagerDefinition(), new Vector2(BodyX, BodyY));
            var wild = new TestNpc(TrialTargetDefinition(), new Vector2(BodyX, BodyY)) { IsWild = true };

            Assert.IsTrue(owned.IsAlive, "precondition: both are alive");
            Assert.IsTrue(wild.IsAlive);
            Assert.IsFalse(owned.Npc.IsWild, "precondition: a spawn point owns the resident");

            var registry = new NpcWorldRegistry();
            registry.Register(owned.Participant);
            registry.Register(wild.Participant);

            var captured = Participant(registry, new FakeSpawner(), VillagerDefinition(), TrialTargetDefinition()).Capture();

            var record = Bodies(captured).Single();
            Assert.AreEqual(TrialNpcId, record.NpcId, "only the NPC nobody re-rolls is written");
        }

        /// <summary>A summon is battle-scoped — no corpse, no world return — so even one that belongs
        /// to no spawn point is never a world fact to restore.</summary>
        [TestMethod]
        public void AWildSummonIsNotWritten()
        {
            var summon = new TestNpc(TrialTargetDefinition(), new Vector2(BodyX, BodyY)) { IsWild = true, IsSummon = true };

            Assert.IsTrue(summon.IsAlive, "precondition: the summon is up");
            Assert.IsTrue(summon.Npc.IsWild, "precondition: no spawn point owns it either");
            Assert.IsTrue(summon.Npc.IsSummon, "precondition: it is a summon");

            var registry = new NpcWorldRegistry();
            registry.Register(summon.Participant);

            var captured = Participant(registry, new FakeSpawner(), TrialTargetDefinition()).Capture();

            Assert.AreEqual(0, Bodies(captured).Count);
        }

        /// <summary>A file written before living NPCs joined the section: it holds bodies only, carries
        /// no modifier list, and every one of its records still restores the way it always did.</summary>
        [TestMethod]
        public void AVersionOneFileStillRestoresItsBodies()
        {
            var definition = UndeadCycleDefinition();
            var spawner = new FakeSpawner();
            var participant = Participant(new NpcWorldRegistry(), spawner, definition);
            var legacy = JToken.Parse($$"""
                {"bodies":[{"kind":"defeated","npcId":"{{RaiderNpcId}}","level":0,"rarity":"Common","stance":"Dexterity",
                "x":{{BodyX}},"y":{{BodyY}},"resurrectDelay":{{SavedDelaySeconds}},"elapsed":{{LainSeconds}},"risingBonus":0.0}]}
                """);

            Assert.IsTrue(participant.Version > LegacySectionVersion, "precondition: the section moved on");

            participant.Restore(legacy, LegacySectionVersion);

            var restored = spawner.Spawned.Single();
            Assert.IsFalse(restored.IsAlive, "the body still lies");
            Assert.AreEqual(NpcLifeStage.Defeated, restored.Lifecycle.Stage);
            Assert.AreEqual(SavedDelaySeconds, restored.Lifecycle.ResurrectDelay, Tolerance);
            Assert.AreEqual(LainSeconds, restored.Lifecycle.Elapsed, Tolerance);
            Assert.AreEqual(BodyX, restored.Position.X, Tolerance);
            Assert.AreEqual(0, restored.Definition.Modifiers.Count, "a body's modifiers re-roll, as they always did");
        }

        /// <summary>
        /// Two loads in a row, which is the only reason the whole construction exists. The trial target
        /// nobody owns has to be in the SECOND snapshot too, or it survives one load and vanishes on the
        /// next. The body a spawn point owned must not: the point restored a resident of its own beside
        /// it, and writing the restored body once it is back on its feet would make that pair permanent.
        /// </summary>
        [TestMethod]
        public void ASecondSaveKeepsTheWildTargetAndLetsAPointsBodyHeal()
        {
            var villager = VillagerDefinition();
            var trial = TrialTargetDefinition();

            var world = new NpcWorldRegistry();
            var target = new TestNpc(trial, new Vector2(BodyX, BodyY)) { IsWild = true };
            var body = new TestNpc(villager, new Vector2(BodyX, BodyY));
            body.LayDown();
            world.Register(target.Participant);
            world.Register(body.Participant);

            Assert.IsTrue(target.Npc.IsWild, "precondition: the quest's target belongs to nobody");
            Assert.IsFalse(body.Npc.IsWild, "precondition: a spawn point owns the resident that fell");
            Assert.AreEqual(NpcLifeStage.Defeated, body.Lifecycle.Stage, "precondition: the resident really lies");

            var first = Participant(world, new FakeSpawner(), villager, trial).Capture();
            Assert.AreEqual(2, Bodies(first).Count, "precondition: the first snapshot holds both");

            // The load: a fresh world, and everything the file puts back enters it like a real spawn.
            var reloaded = new NpcWorldRegistry();
            var spawner = new FakeSpawner(reloaded);
            var participant = Participant(reloaded, spawner, villager, trial);
            participant.Restore(first, participant.Version);

            var restoredBody = spawner.Spawned.Single(npc => npc.Definition.NpcId == VillagerNpcId);
            restoredBody.Lifecycle.Tick(RolledRecoverySeconds);
            Assert.IsTrue(restoredBody.Revived, "precondition: the resident got back up alive");

            var second = Participant(reloaded, new FakeSpawner(), villager, trial).Capture();

            var record = Bodies(second).Single();
            Assert.AreEqual(TrialNpcId, record.NpcId, "the trial target is still there after a second save");
            Assert.AreEqual(NpcBodySaveData.AliveKind, record.Kind);
            Assert.IsFalse(Bodies(second).Any(entry => entry.NpcId == VillagerNpcId),
                "the point's own resident is the point's business again — the world heals its duplicate");
        }

        /// <summary>A modifier the data has since dropped must not take the trial target with it: a
        /// weaker target the quest can still finish beats a stage that never ends.</summary>
        [TestMethod]
        public void AnAliveRecordSurvivesAModifierTheCatalogLost()
        {
            var definition = TrialTargetDefinition();
            var spawner = new FakeSpawner();
            var participant = Participant(new NpcWorldRegistry(), spawner, definition);
            var saved = JToken.FromObject(new NpcWorldSaveData
            {
                Bodies =
                [
                    new NpcBodySaveData
                    {
                        Kind = NpcBodySaveData.AliveKind,
                        NpcId = TrialNpcId,
                        X = BodyX,
                        Y = BodyY,
                        Modifiers = [FirstModifierId, LostModifierId],
                    }
                ]
            });

            participant.Restore(saved, participant.Version);

            var restored = spawner.Spawned.Single(); // the record is not dropped over a lost modifier
            Assert.IsTrue(restored.IsAlive);
            CollectionAssert.AreEqual(new[] { FirstModifierId },
                restored.Definition.Modifiers.Select(modifier => modifier.Id).ToArray(),
                "the known modifier stays, the lost one is reported and skipped");
        }

        private static NpcWorldSaveParticipant Participant(INpcWorldRegistry registry, INpcWorldSpawner spawner, params NpcDefinition[] known) =>
            Participant(registry, spawner, new Mock<INpcPopulationService>(), known);

        private static NpcWorldSaveParticipant Participant(
            INpcWorldRegistry registry, INpcWorldSpawner spawner, Mock<INpcPopulationService> population, params NpcDefinition[] known)
        {
            var provider = new Mock<INpcProvider>();
            provider.SetupGet(p => p.KnownNpcIds).Returns(known.Select(d => d.NpcId).ToArray());
            provider.Setup(p => p.CreateDefinition(It.IsAny<string>(), It.IsAny<NpcDefinitionOverrides?>()))
                .Returns<string, NpcDefinitionOverrides?>((npcId, overrides) =>
                {
                    var definition = known.First(d => d.NpcId == npcId);
                    return definition with
                    {
                        Level = overrides?.Level ?? definition.Level,
                        Rarity = overrides?.Rarity ?? definition.Rarity,
                        Stance = overrides?.Stance ?? definition.Stance,
                    };
                });

            population.Setup(p => p.TryReserve()).Returns(true);

            return new NpcWorldSaveParticipant(registry, provider.Object, ModifierProvider(), population.Object, spawner);
        }

        /// <summary>The catalog the trial's modifiers come from; an id outside it is one the data lost.</summary>
        private static INpcModifierProvider ModifierProvider()
        {
            var provider = new Mock<INpcModifierProvider>();
            provider.Setup(p => p.GetAllModifierIds()).Returns([FirstModifierId, SecondModifierId]);
            provider.Setup(p => p.GetModifier(It.IsAny<string>())).Returns<string>(Modifier);
            return provider.Object;
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

        /// <summary>What the quest spawn action builds: a pinned identity plus the EXACT modifier set
        /// the trial names (SpawnNpcAction replaces the rolled ones the same way).</summary>
        private static NpcDefinition TrialTargetDefinition() => new()
        {
            NpcId = TrialNpcId,
            Level = TrialLevel,
            Rarity = TrialRarity,
            Stance = TrialStance,
            Fraction = Fractions.Human,
            Parameters = new Dictionary<EntityParameter, float>(),
            Abilities = [],
            Modifiers = [Modifier(FirstModifierId), Modifier(SecondModifierId)],
            Lifecycle = new NpcLifecycleConfig(),
        };

        private static INpcModifier Modifier(string id)
        {
            var modifier = new Mock<INpcModifier>();
            modifier.SetupGet(m => m.Id).Returns(id);
            return modifier.Object;
        }

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
                Definition = definition;
                Lifecycle = NpcLifecycleFactory.Create(definition, new FixedRandom());
                Fraction = definition.Fraction;
                Position = position;

                if (Lifecycle is IUndeadRiseLifecycle undead) undead.ResurrectionReady += OnResurrectionReady;
                if (Lifecycle is IAliveRiseLifecycle alive) alive.ReviveReady += OnReviveReady;

                var abilityBook = new Mock<IAbilityBookComponent>();
                abilityBook.SetupGet(b => b.CurrentStance).Returns(definition.Stance);

                // BaseNpc.ApplyDefinition hands the definition's modifiers to the component; the ids
                // the save reads back come from there, not from the definition.
                var modifiers = new Mock<INpcModifiersComponent>();
                modifiers.SetupGet(c => c.AllModifiers).Returns(definition.Modifiers);

                var mock = new Mock<IFightableNpc>();
                mock.SetupGet(n => n.Id).Returns(definition.NpcId);
                mock.SetupGet(n => n.Level).Returns(definition.Level);
                mock.SetupGet(n => n.Rarity).Returns(definition.Rarity);
                mock.SetupGet(n => n.AbilityBook).Returns(abilityBook.Object);
                mock.SetupGet(n => n.NpcModifiers).Returns(modifiers.Object);
                mock.SetupGet(n => n.IsSummon).Returns(() => IsSummon);
                mock.SetupGet(n => n.IsWild).Returns(() => IsWild);
                mock.Setup(n => n.MarkAsWild()).Callback(() => IsWild = true);
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

            public NpcDefinition Definition { get; }
            public INpcLifecycle Lifecycle { get; }
            public IFightableNpc Npc { get; }
            public ISkirmishParticipant Participant { get; }
            public Vector2 Position { get; }
            public bool IsAlive { get; set; } = true;

            /// <summary>Nobody's NPC: an authored order put it here, no spawn point re-rolls it.</summary>
            public bool IsWild { get; set; }

            public bool IsSummon { get; init; }
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

        /// <summary>A registry, when given one, receives every spawned NPC exactly as the world does —
        /// which is what lets a second Capture see what the previous Restore put back.</summary>
        private sealed class FakeSpawner(INpcWorldRegistry? registry = null) : INpcWorldSpawner
        {
            public List<TestNpc> Spawned { get; } = [];

            /// <summary>The node's own load path: ApplyDefinition builds the cycle from the definition.</summary>
            public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position)
            {
                var npc = new TestNpc(definition, position);
                Spawned.Add(npc);
                registry?.Register(npc.Participant);
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
