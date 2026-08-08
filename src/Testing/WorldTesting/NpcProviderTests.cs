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

        /// <summary>The level an authored record names — inside the rollable range, so a definition
        /// carrying it proves the section was read and not that the range had no other value.</summary>
        private const int AuthoredLevel = 7;

        private const int RollableLevelMin = 1;
        private const int RollableLevelMax = 40;

        /// <summary>Spawns drawn when a test judges whether a fact still rolls.</summary>
        private const int RollSamples = 30;

        /// <summary>The type and rarity the modifier-count tests spawn at: together they send the
        /// type × rarity formula to a number that is neither zero nor the count those tests author,
        /// so every result below names the rule that produced it.</summary>
        private const EntityType ModifierRollingType = EntityType.Elit;

        private const Rarity ModifierRollingRarity = Rarity.Mythic;

        /// <summary>The modifier count an authored record names.</summary>
        private const int AuthoredModifiers = 3;

        /// <summary>A count that is not a count — what the file does with nonsense is a decision.</summary>
        private const int NegativeModifiers = -1;

        /// <summary>Modifiers offered to the roll: more than any count named here, so a short result
        /// means the count decided it and never that the pool ran dry.</summary>
        private const int ModifierPoolSize = 8;

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

        /// <summary>The authored spawn: the named villager is placed by hand, not rolled, so two
        /// spawns of one id must hand out the same fighter — the stance, level and rarity the data
        /// names and nothing the dice picked.</summary>
        [TestMethod]
        public void AuthoredSection_HandsOutTheSameFighterTwice_InsteadOfRolling()
        {
            var provider = RollableProvider(new NpcAuthoredData
            {
                Stance = nameof(Stance.Strength),
                Level = AuthoredLevel,
                Rarity = nameof(Rarity.Uncommon),
            });

            var first = provider.CreateDefinition(TestNpcId);
            var second = provider.CreateDefinition(TestNpcId);

            Assert.AreEqual(Stance.Strength, first.Stance, "the authored stance must replace the roll");
            Assert.AreEqual(Stance.Strength, first.Behavior.Stance, "the archetype must follow the authored stance, not a roll of its own");
            Assert.AreEqual(AuthoredLevel, first.Level, "the authored level must replace the roll");
            Assert.AreEqual(Rarity.Uncommon, first.Rarity, "the authored rarity must replace the roll");
            Assert.AreEqual(Stance.Strength, second.Behavior.Stance, "a second spawn of the same id fought by another archetype");
            Assert.AreEqual(first.Stance, second.Stance, "a second spawn of the same id changed stance");
            Assert.AreEqual(first.Level, second.Level, "a second spawn of the same id changed level");
            Assert.AreEqual(first.Rarity, second.Rarity, "a second spawn of the same id changed rarity");
        }

        /// <summary>The precondition of the authored test and the regression of every shipped record:
        /// a record naming no section still rolls all three facts, exactly as it did before authoring
        /// existed. Rolls with these ranges collapsing to one value across the whole batch is beyond
        /// astronomical, so a frozen fact here means the roll was lost, not that luck ran out.</summary>
        [TestMethod]
        public void RecordWithoutTheSection_StillRollsStanceLevelAndRarity()
        {
            var provider = RollableProvider(authored: null);

            var definitions = Enumerable.Range(0, RollSamples).Select(_ => provider.CreateDefinition(TestNpcId)).ToList();

            Assert.IsTrue(definitions.Select(definition => definition.Stance).Distinct().Count() > 1, "the stance stopped rolling");
            Assert.IsTrue(definitions.Select(definition => definition.Level).Distinct().Count() > 1, "the level stopped rolling");
            Assert.IsTrue(definitions.Select(definition => definition.Rarity).Distinct().Count() > 1, "the rarity stopped rolling");
        }

        /// <summary>Partial sections are legal: an author pins the facts that matter and leaves the
        /// rest to the dice. The unnamed facts must keep rolling — a section is not a freeze switch.</summary>
        [TestMethod]
        public void SectionNamingOnlyTheLevel_PinsTheLevelAndLeavesTheRestRolling()
        {
            var provider = RollableProvider(new NpcAuthoredData { Level = AuthoredLevel });

            var definitions = Enumerable.Range(0, RollSamples).Select(_ => provider.CreateDefinition(TestNpcId)).ToList();

            Assert.IsTrue(definitions.All(definition => definition.Level == AuthoredLevel), "the named level must be fixed");
            Assert.IsTrue(definitions.Select(definition => definition.Stance).Distinct().Count() > 1, "an unnamed stance must keep rolling");
            Assert.IsTrue(definitions.Select(definition => definition.Rarity).Distinct().Count() > 1, "an unnamed rarity must keep rolling");
        }

        /// <summary>A fixed rarity has two homes — the older top-level field and the authored block.
        /// The block is meant to read as the whole truth about a named npc, so it wins.</summary>
        [TestMethod]
        public void AuthoredRarity_BeatsTheTopLevelRarityField()
        {
            var provider = LoadedProvider(Npc() with
            {
                Rarity = nameof(Rarity.Rare),
                Authored = new NpcAuthoredData { Rarity = nameof(Rarity.Mythic) },
            });

            Assert.AreEqual(Rarity.Mythic, provider.CreateDefinition(TestNpcId).Rarity);
        }

        [TestMethod]
        public void MisspelledAuthoredStance_IsRefused_NotSilentlyRolled()
        {
            var provider = LoadedProvider(Npc() with { Authored = new NpcAuthoredData { Stance = "Dexterety" } });

            Assert.ThrowsException<FormatException>(() => provider.CreateDefinition(TestNpcId));
        }

        [TestMethod]
        public void MisspelledAuthoredRarity_IsRefused_NotSilentlyRolled()
        {
            var provider = LoadedProvider(Npc() with { Authored = new NpcAuthoredData { Rarity = "Legendery" } });

            Assert.ThrowsException<FormatException>(() => provider.CreateDefinition(TestNpcId));
        }

        /// <summary>The reason the field exists: an authored villager is allowed to be a person and
        /// not a rolled encounter. Zero must exclude the pick itself and not merely trim its result —
        /// and since every weighted pick this provider makes appends a modifier, an empty list is
        /// exactly the claim that no pick ran. The first assert is the precondition: the very same
        /// record, asked without the field, does come out wearing modifiers.</summary>
        [TestMethod]
        public void AuthoredModifierCountOfZero_LeavesTheNpcBare_WhereTheSameRecordRollsModifiers()
        {
            var rolling = ModifierRollingProvider(authored: null);
            var bare = ModifierRollingProvider(new NpcAuthoredData { ModifierCount = 0 });

            Assert.IsTrue(rolling.CreateDefinition(TestNpcId).Modifiers.Count > 0, "precondition: this record rolls modifiers while the section names no count");
            Assert.AreEqual(0, bare.CreateDefinition(TestNpcId).Modifiers.Count, "an authored count of zero must exclude the roll entirely");
        }

        /// <summary>A named count is the whole answer: the type × rarity table does not get to add
        /// to it, subtract from it or be consulted at all.</summary>
        [TestMethod]
        public void AuthoredModifierCount_ReplacesTheTypeAndRarityFormula()
        {
            var provider = ModifierRollingProvider(new NpcAuthoredData { ModifierCount = AuthoredModifiers });

            Assert.AreNotEqual(
                AuthoredModifiers,
                NpcTypeDefaults.ModifierCount(ModifierRollingType, ModifierRollingRarity),
                "this test proves nothing while the formula happens to answer the authored number");
            Assert.AreEqual(AuthoredModifiers, provider.CreateDefinition(TestNpcId).Modifiers.Count);
        }

        /// <summary>The regression every shipped record rides on: with no count named, the number of
        /// modifiers is still the agreed type × rarity table and the section changed nothing.</summary>
        [TestMethod]
        public void RecordWithoutTheModifierCount_KeepsTheTypeAndRarityFormula()
        {
            var provider = ModifierRollingProvider(authored: null);

            Assert.AreEqual(
                NpcTypeDefaults.ModifierCount(ModifierRollingType, ModifierRollingRarity),
                provider.CreateDefinition(TestNpcId).Modifiers.Count);
        }

        /// <summary>A negative number is not a count of anything. The record still spoke, so the
        /// formula stays out of it and the spawn comes out bare — the only reading a spawn can act on,
        /// and the quiet one (falling back to the roll) would hand modifiers to the author who was
        /// trying to take them away.</summary>
        [TestMethod]
        public void NegativeAuthoredModifierCount_LeavesTheNpcBare_InsteadOfFallingBackToTheFormula()
        {
            var provider = ModifierRollingProvider(new NpcAuthoredData { ModifierCount = NegativeModifiers });

            Assert.AreEqual(0, provider.CreateDefinition(TestNpcId).Modifiers.Count);
        }

        /// <summary>A provider holding one NPC and the archetype of the stance it rolls.</summary>
        private static NpcProvider LoadedProvider(NpcData npc)
        {
            var provider = CreateProvider([PoolAbility]);
            provider.Apply(DataCatalog.Npc, NpcFile(npc));
            provider.Apply(DataCatalog.NpcBehaviors, BehaviorFile(Archetype(nameof(Stance.Dexterity))));
            return provider;
        }

        /// <summary>A provider whose only NPC has every fact left to the dice — a wide level range,
        /// all three stances (each with its archetype) and no fixed rarity — so anything that comes
        /// out frozen came out of the authored section and nowhere else.</summary>
        private static NpcProvider RollableProvider(NpcAuthoredData? authored)
        {
            var npc = Npc() with
            {
                Stances = [nameof(Stance.Dexterity), nameof(Stance.Strength), nameof(Stance.Intelligence)],
                LevelMin = RollableLevelMin,
                LevelMax = RollableLevelMax,
                Rarity = null,
                Authored = authored,
            };

            var provider = CreateProvider([PoolAbility]);
            provider.Apply(DataCatalog.Npc, NpcFile(npc));
            provider.Apply(DataCatalog.NpcBehaviors, BehaviorFile(
                Archetype(nameof(Stance.Dexterity)),
                Archetype(nameof(Stance.Strength)),
                Archetype(nameof(Stance.Intelligence))));
            return provider;
        }

        /// <summary>A provider whose modifier catalog is stocked (Elit × Mythic, so the formula asks
        /// for several) and whose only NPC carries the section under test — the number of modifiers
        /// its definition comes out with is then the count rule and nothing else.</summary>
        private static NpcProvider ModifierRollingProvider(NpcAuthoredData? authored)
        {
            var npc = Npc() with
            {
                EntityType = ModifierRollingType.ToString(),
                Rarity = ModifierRollingRarity.ToString(),
                Authored = authored,
            };

            var provider = CreateProvider([PoolAbility], ModifierPool());
            provider.Apply(DataCatalog.Npc, NpcFile(npc));
            provider.Apply(DataCatalog.NpcBehaviors, BehaviorFile(Archetype(nameof(Stance.Dexterity))));
            return provider;
        }

        /// <summary>Pickable modifiers of equal weight; an empty pool (the default) never reaches the
        /// weighted roll, which is all the other tests need from the catalog.</summary>
        private static List<INpcModifier> ModifierPool() =>
            [.. Enumerable.Range(0, ModifierPoolSize).Select(index => PoolModifier($"Modifier_{index}"))];

        private static INpcModifier PoolModifier(string id)
        {
            var modifier = new Mock<INpcModifier>();
            modifier.SetupGet(entry => entry.Id).Returns(id);
            modifier.SetupGet(entry => entry.Weight).Returns(1f);
            return modifier.Object;
        }

        private static NpcProvider CreateProvider(IReadOnlyCollection<string> abilityIds, IReadOnlyList<INpcModifier>? modifierPool = null)
        {
            var abilities = new Mock<IAbilityProvider>();
            abilities.SetupGet(provider => provider.KnownAbilityIds).Returns(abilityIds);
            abilities.Setup(provider => provider.CreateAbility(It.IsAny<string>())).Returns(() => Mock.Of<IAbility>());

            var modifiers = new Mock<INpcModifierProvider>();
            modifiers.Setup(provider => provider.GetAllModifiers()).Returns(modifierPool ?? []);
            modifiers.Setup(provider => provider.GetModifier(It.IsAny<string>())).Returns(() => Mock.Of<INpcModifier>());

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
