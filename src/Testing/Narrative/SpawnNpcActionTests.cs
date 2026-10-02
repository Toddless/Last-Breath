namespace LastBreathTest.Narrative
{
    using Core.Ai.World.Raids;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Data.SaveData;
    using Core.Entity;
    using Core.Enums;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Services;
    using Godot;
    using Moq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The narrative "put an NPC in the world" action: a trial target must arrive as the exact
    /// fighter the quest names, at the place the quest names, even in a world already at its
    /// population cap — and refuse loudly rather than half-spawn on a typo.
    /// </summary>
    [TestClass]
    public class SpawnNpcActionTests
    {
        private const string TargetNpcId = "Npc_Trial_Brute";
        private const string PointId = "Trial_Grounds";
        private const string ScaleModifierId = "ScaleModifier";
        private const string EffectsModifierId = "ItemEffectsModifier";

        private static readonly Vector2 s_pointPosition = new(1300, -420);

        private GameEventBus _bus = null!;
        private NpcPopulationService _population = null!;
        private SpawnPointRegistry _points = null!;
        private FakeSpawner _spawner = null!;
        private Mock<INpcProvider> _npcs = null!;
        private Mock<INpcModifierProvider> _modifiers = null!;

        [TestInitialize]
        public void Setup()
        {
            _bus = new GameEventBus();
            _population = new NpcPopulationService(_bus);
            _points = new SpawnPointRegistry();
            _points.Register(new FakePoint(PointId, s_pointPosition));
            _spawner = new FakeSpawner();

            _npcs = new Mock<INpcProvider>();
            _npcs.SetupGet(provider => provider.KnownNpcIds).Returns([TargetNpcId]);
            _npcs.Setup(provider => provider.CreateDefinition(It.IsAny<string>())).Returns<string>(RolledDefinition);

            _modifiers = new Mock<INpcModifierProvider>();
            _modifiers.Setup(provider => provider.GetAllModifierIds()).Returns([ScaleModifierId, EffectsModifierId]);
            _modifiers.Setup(provider => provider.GetModifier(It.IsAny<string>()))
                .Returns<string>(id => Mock.Of<INpcModifier>(modifier => modifier.Id == id));
        }

        [TestMethod]
        public void SpawnsTheNamedNpcWithTheNamedModifiersAtTheNamedPoint()
        {
            Build(TargetNpcId, PointId, [ScaleModifierId, EffectsModifierId]).Execute(NarrativeContext.Empty);

            var spawned = _spawner.Spawned.Single();
            Assert.AreEqual(TargetNpcId, spawned.Definition.NpcId);
            Assert.AreEqual(s_pointPosition, spawned.Position, "the target must arrive at the point the quest named");
            CollectionAssert.AreEqual(
                new[] { ScaleModifierId, EffectsModifierId },
                spawned.Definition.Modifiers.Select(modifier => modifier.Id).ToArray(),
                "a trial fought against another modifier set is another trial");
        }

        [TestMethod]
        public void AbsentModifiersListLeavesTheRecordsOwnRoll()
        {
            Build(TargetNpcId, PointId, modifierIds: null).Execute(NarrativeContext.Empty);

            CollectionAssert.AreEqual(
                new[] { ScaleModifierId },
                _spawner.Spawned.Single().Definition.Modifiers.Select(modifier => modifier.Id).ToArray(),
                "naming no modifiers must not strip the ones the record rolled");
        }

        [TestMethod]
        public void EmptyModifiersListStripsTheRolledOnes()
        {
            Build(TargetNpcId, PointId, []).Execute(NarrativeContext.Empty);

            Assert.AreEqual(0, _spawner.Spawned.Single().Definition.Modifiers.Count,
                "an authored empty set is the weakest rung of the ladder, not a missing answer");
        }

        [TestMethod]
        public void TargetIgnoresTheFullWorldAndStillOccupiesASlot()
        {
            _population.GlobalLimit = 0;

            Build(TargetNpcId, PointId, [ScaleModifierId]).Execute(NarrativeContext.Empty);

            Assert.AreEqual(1, _spawner.Spawned.Count, "a quest target must never be lost to the population cap");
            Assert.AreEqual(1, _population.CurrentCount, "the target still occupies the counter — spawn points pause instead");
        }

        [TestMethod]
        public void WorldlessSpawnReservesNothing()
        {
            var action = new SpawnNpcAction(_npcs.Object, _modifiers.Object, new NullSpawner(), _population, _points,
                TargetNpcId, PointId, [ScaleModifierId]);

            action.Execute(NarrativeContext.Empty);

            Assert.AreEqual(0, _population.CurrentCount, "the slot is taken only by an NPC that actually reached the world");
        }

        [TestMethod]
        public void UnknownNpcIdRefusesTheSpawn()
        {
            Build("Npc_Typo", PointId, [ScaleModifierId]).Execute(NarrativeContext.Empty);

            Assert.AreEqual(0, _spawner.Spawned.Count);
            Assert.AreEqual(0, _population.CurrentCount);
        }

        [TestMethod]
        public void UnknownModifierIdRefusesTheWholeSpawn()
        {
            Build(TargetNpcId, PointId, [ScaleModifierId, "Modifier_Typo"]).Execute(NarrativeContext.Empty);

            Assert.AreEqual(0, _spawner.Spawned.Count, "a partially armed trial target must not reach the world");
            Assert.AreEqual(0, _population.CurrentCount);
        }

        [TestMethod]
        public void UnknownPointIdRefusesTheSpawn()
        {
            Build(TargetNpcId, "Nowhere", [ScaleModifierId]).Execute(NarrativeContext.Empty);

            Assert.AreEqual(0, _spawner.Spawned.Count);
            Assert.AreEqual(0, _population.CurrentCount);
        }

        [TestMethod]
        public void PointWithoutAWorldPositionRefusesTheSpawn()
        {
            const string blindId = "Blind_Point";
            _points.Register(new BlindPoint(blindId));

            Build(TargetNpcId, blindId, [ScaleModifierId]).Execute(NarrativeContext.Empty);

            Assert.AreEqual(0, _spawner.Spawned.Count);
        }

        [TestMethod]
        public void FactoryParsesTheTrialEntry()
        {
            var action = Factory().Create(JObject.Parse(
                $$"""{"type": "SpawnNpc", "npcId": "{{TargetNpcId}}", "pointId": "{{PointId}}", "modifiers": ["{{ScaleModifierId}}"]}"""), null!);

            Assert.IsNotNull(action);
            action.Execute(NarrativeContext.Empty);
            CollectionAssert.AreEqual(
                new[] { ScaleModifierId },
                _spawner.Spawned.Single().Definition.Modifiers.Select(modifier => modifier.Id).ToArray());
        }

        [TestMethod]
        public void FactoryRefusesEntriesWithoutAnNpcOrAPlace()
        {
            Assert.IsNull(Factory().Create(JObject.Parse($$"""{"type": "SpawnNpc", "pointId": "{{PointId}}"}"""), null!),
                "npcId is required");
            Assert.IsNull(Factory().Create(JObject.Parse($$"""{"type": "SpawnNpc", "npcId": "{{TargetNpcId}}"}"""), null!),
                "pointId is required");
            Assert.IsNull(Factory().Create(JObject.Parse(
                    $$"""{"type": "SpawnNpc", "npcId": "{{TargetNpcId}}", "pointId": "{{PointId}}", "modifiers": [""]}"""), null!),
                "an empty modifier id is a typo, not a bare target");
        }

        private SpawnNpcActionFactory Factory() =>
            new(_npcs.Object, _modifiers.Object, _spawner, _population, _points);

        private SpawnNpcAction Build(string npcId, string pointId, IReadOnlyList<string>? modifierIds) =>
            new(_npcs.Object, _modifiers.Object, _spawner, _population, _points, npcId, pointId, modifierIds);

        /// <summary>What the catalog hands out on its own: one rolled modifier the trial may replace.</summary>
        private static NpcDefinition RolledDefinition(string npcId) => new()
        {
            NpcId = npcId,
            Fraction = Fractions.Human,
            Parameters = new Dictionary<EntityParameter, float>(),
            Abilities = [],
            Modifiers = [Mock.Of<INpcModifier>(modifier => modifier.Id == ScaleModifierId)],
            Lifecycle = new Core.Ai.World.NpcLifecycleConfig(),
        };

        private sealed record SpawnRecord(NpcDefinition Definition, Vector2 Position);

        private sealed class FakeSpawner : INpcWorldSpawner
        {
            public List<SpawnRecord> Spawned { get; } = [];

            public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position)
            {
                Spawned.Add(new SpawnRecord(definition, position));
                return Mock.Of<IFightableNpc>();
            }

            public void Despawn(IFightableNpc npc)
            {
            }
        }

        /// <summary>No world to spawn into (the spawner's documented null answer).</summary>
        private sealed class NullSpawner : INpcWorldSpawner
        {
            public IFightableNpc? Spawn(NpcDefinition definition, Vector2 position) => null;

            public void Despawn(IFightableNpc npc)
            {
            }
        }

        /// <summary>A scene spawn point as the action sees it: a save identity plus a world position.</summary>
        private sealed class FakePoint(string id, Vector2 position) : IPersistentSpawnPoint, IRaidSpawnSite
        {
            public string PointId => id;
            public Fractions? Fraction => Fractions.Human;
            public Vector2 Position => position;
            public IReadOnlyList<string> NpcIds => [];

            public SpawnPointSaveData CaptureState() => new() { Id = id };

            public void RestoreState(SpawnPointSaveData data)
            {
            }

            public void FillFresh()
            {
            }
        }

        /// <summary>A registered point that reports no world position (outside the raid-site contract).</summary>
        private sealed class BlindPoint(string id) : IPersistentSpawnPoint
        {
            public string PointId => id;

            public SpawnPointSaveData CaptureState() => new() { Id = id };

            public void RestoreState(SpawnPointSaveData data)
            {
            }

            public void FillFresh()
            {
            }
        }
    }
}
