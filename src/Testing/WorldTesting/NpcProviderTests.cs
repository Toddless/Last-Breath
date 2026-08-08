namespace LastBreathTest.WorldTesting
{
    using Core.Ai;
    using Core.Ai.World;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.GameData;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Enums;
    using Moq;
    using Newtonsoft.Json;

    /// <summary>
    /// The NPC provider is the single source of spawn-ready definitions: it eats the Npc and
    /// NpcBehaviors catalogs and rolls the identity of one NPC. The tests pin what a spawn is
    /// allowed to do with bad content — a record the provider cannot fully build is refused, never
    /// handed out half-rolled — and that the shipped catalogs still come through it.
    /// </summary>
    [TestClass]
    public class NpcProviderTests
    {
        private const string TestNpcId = "Npc_Test";
        private const string PoolAbility = "Ability_Pool";

        [TestMethod]
        public void ShippedCatalogs_LoadAndSpawnEveryAuthoredNpc()
        {
            var provider = CreateProvider(ShippedAbilityIds());
            provider.Apply(DataCatalog.Npc, ShippedFile(DataCatalog.Npc, "Npc.json"));
            provider.Apply(DataCatalog.NpcBehaviors, ShippedFile(DataCatalog.NpcBehaviors, "NpcBehavior.json"));

            Assert.IsTrue(provider.KnownNpcIds.Count > 0, "the shipped Npc catalog must reach the provider");

            foreach (string npcId in provider.KnownNpcIds)
            {
                var definition = provider.CreateDefinition(npcId);

                Assert.AreEqual(npcId, definition.NpcId);
                Assert.IsTrue(definition.Level > 0, $"'{npcId}' rolled level {definition.Level}");
                Assert.IsNotNull(definition.Behavior, $"'{npcId}' spawned without a behavior archetype");
                Assert.AreEqual(definition.Stance, definition.Behavior.Stance, $"'{npcId}' got an archetype of another stance");
                Assert.IsTrue(definition.Parameters.Count > 0, $"'{npcId}' spawned without base parameters");
            }
        }

        [TestMethod]
        public void UnknownNpcId_IsRefused()
        {
            var provider = LoadedProvider(Npc());

            Assert.ThrowsException<KeyNotFoundException>(() => provider.CreateDefinition("Npc_Nobody"));
        }

        [TestMethod]
        public void StanceWithoutAnArchetype_IsRefused()
        {
            var provider = CreateProvider([]);
            provider.Apply(DataCatalog.Npc, NpcFile(Npc()));
            provider.Apply(DataCatalog.NpcBehaviors, BehaviorFile(Archetype(nameof(Stance.Strength))));

            Assert.ThrowsException<KeyNotFoundException>(() => provider.CreateDefinition(TestNpcId));
        }

        [TestMethod]
        public void MisspelledEnumValue_IsRefused_NotSilentlyDefaulted()
        {
            var provider = LoadedProvider(Npc() with { Fraction = "Elves" });

            Assert.ThrowsException<FormatException>(() => provider.CreateDefinition(TestNpcId));
        }

        [TestMethod]
        public void MisspelledScheduleTime_IsRefused()
        {
            var world = new NpcWorldData
            {
                Schedule = [new NpcScheduleSlotData { From = "22-00", To = "06:00", Activity = nameof(WorldActivityType.Sleep) }],
            };
            var provider = LoadedProvider(Npc() with { World = world });

            Assert.ThrowsException<FormatException>(() => provider.CreateDefinition(TestNpcId));
        }

        [TestMethod]
        public void FileThatIsNotACatalog_IsRefused()
        {
            var provider = CreateProvider([]);

            Assert.ThrowsException<InvalidOperationException>(
                () => provider.Apply(DataCatalog.Npc, new GameDataFile("Npc.json", "null")));
        }

        [TestMethod]
        public void AuthoredAbilityNobodyRegistered_CostsItsOwnLine()
        {
            var provider = LoadedProvider(Npc() with { Abilities = ["Ability_Gone", PoolAbility] });

            var definition = provider.CreateDefinition(TestNpcId);

            Assert.AreEqual(1, definition.Abilities.Count, "a typo in one authored id must not cost the whole spawn");
        }

        [TestMethod]
        public void Overrides_PinIdentityInsteadOfRolling()
        {
            var provider = LoadedProvider(Npc() with { LevelMin = 1, LevelMax = 40, Rarity = null });
            var overrides = new NpcDefinitionOverrides { Level = 27, Rarity = Rarity.Legendary, Stance = Stance.Dexterity };

            var definition = provider.CreateDefinition(TestNpcId, overrides);

            Assert.AreEqual(27, definition.Level);
            Assert.AreEqual(Rarity.Legendary, definition.Rarity);
            Assert.AreEqual(Stance.Dexterity, definition.Stance);
        }

        [TestMethod]
        public void LifecycleWithoutAKind_StaysTheUndeadCycleWithItsOldNumbers()
        {
            var lifecycle = new NpcLifecycleData { ResurrectMinSeconds = 20f, ResurrectMaxSeconds = 90f, MaxStrengthBonus = 0.5f };
            var provider = LoadedProvider(Npc() with { Lifecycle = lifecycle });

            var definition = provider.CreateDefinition(TestNpcId);

            Assert.AreEqual(NpcLifecycleKind.Undead, definition.LifecycleKind, "a section naming no kind must keep the old fate");
            Assert.AreEqual(20f, definition.Lifecycle.ResurrectMinSeconds);
            Assert.AreEqual(90f, definition.Lifecycle.ResurrectMaxSeconds);
            Assert.AreEqual(0.5f, definition.Lifecycle.MaxStrengthBonus);
        }

        [TestMethod]
        public void NoLifecycleSectionAtAll_StaysTheUndeadCycleWithTheDesignDefaults()
        {
            var provider = LoadedProvider(Npc());

            var definition = provider.CreateDefinition(TestNpcId);
            var defaults = new NpcLifecycleConfig();

            Assert.AreEqual(NpcLifecycleKind.Undead, definition.LifecycleKind);
            Assert.AreEqual(defaults.ResurrectMinSeconds, definition.Lifecycle.ResurrectMinSeconds);
            Assert.AreEqual(defaults.ResurrectMaxSeconds, definition.Lifecycle.ResurrectMaxSeconds);
            Assert.AreEqual(defaults.MaxStrengthBonus, definition.Lifecycle.MaxStrengthBonus);
        }

        [TestMethod]
        public void VillagerKind_CarriesTheVillagerCycleAndItsOwnTimers()
        {
            var lifecycle = new NpcLifecycleData
            {
                Kind = nameof(NpcLifecycleKind.Villager),
                RecoverMinSeconds = 5f,
                RecoverMaxSeconds = 9f,
            };
            var provider = LoadedProvider(Npc() with { Lifecycle = lifecycle });

            var definition = provider.CreateDefinition(TestNpcId);

            Assert.AreEqual(NpcLifecycleKind.Villager, definition.LifecycleKind);
            Assert.AreEqual(5f, definition.VillagerLifecycle.RecoverMinSeconds, "the villager reads its own recovery timers");
            Assert.AreEqual(9f, definition.VillagerLifecycle.RecoverMaxSeconds);
        }

        [TestMethod]
        public void VillagerWithoutTimers_TakesTheVillagerDefaults_NotTheUndeadOnes()
        {
            var provider = LoadedProvider(Npc() with { Lifecycle = new NpcLifecycleData { Kind = nameof(NpcLifecycleKind.Villager) } });

            var definition = provider.CreateDefinition(TestNpcId);
            var defaults = new VillagerLifecycleConfig();

            Assert.AreEqual(NpcLifecycleKind.Villager, definition.LifecycleKind);
            Assert.AreEqual(defaults.RecoverMinSeconds, definition.VillagerLifecycle.RecoverMinSeconds);
            Assert.AreEqual(defaults.RecoverMaxSeconds, definition.VillagerLifecycle.RecoverMaxSeconds);
        }

        [TestMethod]
        public void MisspelledLifecycleKind_IsRefused_NotSilentlyUndead()
        {
            var provider = LoadedProvider(Npc() with { Lifecycle = new NpcLifecycleData { Kind = "Vilager" } });

            Assert.ThrowsException<FormatException>(() => provider.CreateDefinition(TestNpcId));
        }

        /// <summary>The shipped records predate the villager cycle: none of them names a kind, and every
        /// one of them must still come out of the provider on the undead cycle it always had.</summary>
        [TestMethod]
        public void ShippedRecordsNamingNoKind_KeepTheUndeadCycle()
        {
            var provider = CreateProvider(ShippedAbilityIds());
            provider.Apply(DataCatalog.Npc, ShippedFile(DataCatalog.Npc, "Npc.json"));
            provider.Apply(DataCatalog.NpcBehaviors, ShippedFile(DataCatalog.NpcBehaviors, "NpcBehavior.json"));
            var shipped = JsonConvert.DeserializeObject<NpcsData>(ShippedFile(DataCatalog.Npc, "Npc.json").Json)!;

            foreach (var npc in shipped.Npcs.Where(npc => string.IsNullOrEmpty(npc.Lifecycle?.Kind)))
                Assert.AreEqual(NpcLifecycleKind.Undead, provider.CreateDefinition(npc.Id).LifecycleKind, $"'{npc.Id}' changed its post-defeat fate");
        }

        [TestMethod]
        public void AuthoredRarity_BeatsTheWeightedRoll()
        {
            var provider = LoadedProvider(Npc() with { Rarity = nameof(Rarity.Mythic) });

            Assert.AreEqual(Rarity.Mythic, provider.CreateDefinition(TestNpcId).Rarity);
        }

        /// <summary>A provider holding one NPC and the archetype of the stance it rolls.</summary>
        private static NpcProvider LoadedProvider(NpcData npc)
        {
            var provider = CreateProvider([PoolAbility]);
            provider.Apply(DataCatalog.Npc, NpcFile(npc));
            provider.Apply(DataCatalog.NpcBehaviors, BehaviorFile(Archetype(nameof(Stance.Dexterity))));
            return provider;
        }

        private static NpcProvider CreateProvider(IReadOnlyCollection<string> abilityIds)
        {
            var abilities = new Mock<IAbilityProvider>();
            abilities.SetupGet(provider => provider.KnownAbilityIds).Returns(abilityIds);
            abilities.Setup(provider => provider.CreateAbility(It.IsAny<string>())).Returns(() => Mock.Of<IAbility>());

            var modifiers = new Mock<INpcModifierProvider>();
            modifiers.Setup(provider => provider.GetAllModifiers()).Returns([]);

            return new NpcProvider(abilities.Object, modifiers.Object);
        }

        private static NpcData Npc() => new()
        {
            Id = TestNpcId,
            Fraction = nameof(Fractions.Animal),
            EntityType = nameof(EntityType.Regular),
            AiIntellect = nameof(AiIntellect.Simple),
            Stances = [nameof(Stance.Dexterity)],
            LevelMin = 3,
            LevelMax = 3,
            Rarity = nameof(Rarity.Rare),
            BaseParameters = new Dictionary<string, float> { [nameof(EntityParameter.Health)] = 100f },
        };

        private static NpcBehaviorData Archetype(string stance) => new()
        {
            Id = $"Behavior_{stance}",
            Stance = stance,
            Abilities = [new NpcAbilityBehaviorData { Id = PoolAbility, Weight = 1f, Role = nameof(AbilityRole.Damage) }],
        };

        private static GameDataFile NpcFile(params NpcData[] npcs) =>
            new("Npc.json", JsonConvert.SerializeObject(new NpcsData { Npcs = [.. npcs] }));

        private static GameDataFile BehaviorFile(params NpcBehaviorData[] behaviors) =>
            new("NpcBehavior.json", JsonConvert.SerializeObject(new NpcBehaviorsData { Behaviors = [.. behaviors] }));

        private static GameDataFile ShippedFile(string catalog, string fileName) =>
            new(fileName, File.ReadAllText(Path.Combine(SharedData.Catalog(catalog), fileName)));

        /// <summary>Every ability id the shipped catalogs name, so a definition is judged by what the
        /// provider builds and not by which abilities a test happens to register.</summary>
        private static HashSet<string> ShippedAbilityIds()
        {
            var npcs = JsonConvert.DeserializeObject<NpcsData>(ShippedFile(DataCatalog.Npc, "Npc.json").Json)!;
            var behaviors = JsonConvert.DeserializeObject<NpcBehaviorsData>(ShippedFile(DataCatalog.NpcBehaviors, "NpcBehavior.json").Json)!;

            HashSet<string> ids = [.. behaviors.Behaviors.SelectMany(behavior => behavior.Abilities).Select(entry => entry.Id)];
            ids.UnionWith(npcs.Npcs.SelectMany(npc => npc.Abilities));
            ids.UnionWith(npcs.Npcs.SelectMany(npc => npc.Stages).SelectMany(stage => stage.Abilities));
            return ids;
        }
    }
}
