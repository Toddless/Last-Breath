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
    using Core.Services;
    using Microsoft.Extensions.DependencyInjection;
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

        /// <summary>Abilities the stance archetype offers, for the same reason as
        /// <see cref="ModifierPoolSize"/> — and it is also what a boss taking the WHOLE pool comes out with.</summary>
        private const int AbilityPoolSize = 8;

        /// <summary>The owner's calibration point for the slot dice: an elite of epic rarity, four
        /// modifier slots and four ability slots.</summary>
        private const EntityType CalibrationType = EntityType.Elit;

        private const Rarity CalibrationRarity = Rarity.Epic;

        /// <summary>Spawns a statistical claim is judged over. Large enough that the intervals asserted
        /// sit many standard deviations away from their edges, so the seed below is a convenience and not
        /// the thing holding the test green.</summary>
        private const int StatisticSamples = 10000;

        /// <summary>One fixed seed for every distribution below: the numbers a run produces are then the
        /// same on every machine and in every process, and a failure names the ladder rather than luck.</summary>
        private const int CalibrationSeed = 20260828;

        /// <summary>The ability count an authored record names — more than the slot dice of a Regular
        /// would ever hand out, so the result says the dice were skipped.</summary>
        private const int AuthoredAbilities = 3;

        /// <summary>Modifiers offered when a SHIPPED record is spawned: deeper than the deepest shipped
        /// ceiling (the Rat King's eight), so the catalog never decides a count.</summary>
        private const int DeepModifierPoolSize = 16;

        /// <summary>The shipped record every boss id begins with; the four of them carry the authored
        /// modifier count that keeps a probabilistic ladder off a hand-balanced fight.</summary>
        private const string BossIdPrefix = "Npc_Boss_";

        /// <summary>The shipped record that names an ability count of zero and means it.</summary>
        private const string ShippedDummyId = "Npc_Dummy";

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

        /// <summary>#224: the definition carries the very growth factor its parameters were scaled by —
        /// 1 + (level − 1) × levelScaling — so the buff binder can put the same growth on flat buff lines.
        /// Level 25 at scaling 0.05 is the calibration point (×2.2); a level-1 spawn carries exactly 1,
        /// which is the promise that nothing changes until the bearer outgrows level 1.</summary>
        [TestMethod]
        public void TheDefinitionCarriesTheLevelFactorItsParametersWereScaledBy()
        {
            var provider = LoadedProvider(Npc() with { LevelMin = 25, LevelMax = 25, LevelScaling = 0.05f });

            Assert.AreEqual(2.2f, provider.CreateDefinition(TestNpcId).LevelFactor, 0.0001f);
        }

        [TestMethod]
        public void ALevelOneSpawnCarriesALevelFactorOfExactlyOne()
        {
            var provider = LoadedProvider(Npc() with { LevelMin = 1, LevelMax = 1, LevelScaling = 0.05f });

            Assert.AreEqual(1f, provider.CreateDefinition(TestNpcId).LevelFactor, 0f);
        }

        /// <summary>The one shipped record that names a count of zero, now that zero means what it says.
        /// A training dummy is a sack to hit: it stands there, it does not cast. Before the two zeroes were
        /// read alike it quietly learned two casts from the Regular rule, which nobody asked it for.</summary>
        [TestMethod]
        public void TheShippedTrainingDummy_StandsThereWithoutASingleCast()
        {
            var provider = ShippedProvider();

            var counts = Enumerable.Range(0, RollSamples).Select(_ => provider.CreateDefinition(ShippedDummyId).Abilities.Count);

            Assert.IsTrue(counts.All(count => count == 0), "the training dummy learned a cast");
        }

        /// <summary>The bosses were handed an authored modifier count when the slot dice arrived, so that a
        /// probabilistic ladder could not quietly take modifiers off a fight that was balanced with all of
        /// them. This is that promise, checked against the very formula the numbers were copied from — and
        /// against what the provider then actually hands out.</summary>
        [TestMethod]
        public void EveryShippedBoss_KeepsTheFullModifierCountItsTypeAndRarityUsedToGuarantee()
        {
            var shipped = JsonConvert.DeserializeObject<NpcsData>(ShippedFile(DataCatalog.Npc, "Npc.json").Json)!;
            var bosses = shipped.Npcs.Where(npc => npc.Id.StartsWith(BossIdPrefix, StringComparison.Ordinal)).ToList();
            var provider = ShippedProvider();

            Assert.IsTrue(bosses.Count > 0, "no boss records were found — the guard is checking nothing");

            foreach (var boss in bosses)
            {
                int ceiling = NpcTypeDefaults.ModifierCount(
                    EnumParser.ParseEnum<EntityType>(boss.EntityType),
                    EnumParser.ParseEnum<Rarity>(boss.Rarity!));

                Assert.AreEqual(ceiling, boss.Authored?.ModifierCount,
                    $"'{boss.Id}' names a modifier count that is not the type × rarity number it used to be guaranteed");
                Assert.AreEqual(ceiling, provider.CreateDefinition(boss.Id).Modifiers.Count,
                    $"'{boss.Id}' spawned with fewer modifiers than its record names");
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

        /// <summary>The contract issue #223 replaced the old one with: with no count named, the type ×
        /// rarity table is the CEILING on how many modifier slots exist, not a promise that every one of
        /// them is filled. A spawn takes what the falling slot chances hand it — never more than the
        /// ceiling, rarely all of it, and not the same number twice.</summary>
        [TestMethod]
        public void RecordWithoutTheModifierCount_TreatsTheTypeAndRarityFormulaAsACeiling()
        {
            int ceiling = NpcTypeDefaults.ModifierCount(CalibrationType, CalibrationRarity);
            var counts = ModifierCounts(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed));

            Assert.IsTrue(counts.All(count => count <= ceiling),
                $"a spawn wore {counts.Max()} modifiers where the type × rarity ceiling is {ceiling}");
            Assert.IsTrue(counts.Distinct().Count() > 1, "the count stopped rolling — the ceiling became a promise again");
            Assert.IsTrue(counts.Contains(ceiling), "the ceiling is never reached, so it is not the ceiling of anything");
        }

        /// <summary>The owner's calibration point, in numbers: an elite of epic rarity wears at least one
        /// modifier almost always and all four of its slots almost never. Judged over
        /// <see cref="StatisticSamples"/> spawns of one seeded generator, so the intervals are a contract
        /// about the ladder and not a coin toss — the counts below sit near the middle of them.
        /// <para>
        /// The ceiling of the interval on all four slots is 4.5% rather than the 7% the calibration talks
        /// about, and that is deliberate: a ladder with its decay thrown away (every later slot as likely
        /// as the second) lands on 5.8% here, and 7% would have waved it through. The shape of the tail is
        /// part of this claim, not only its end.
        /// </para></summary>
        [TestMethod]
        public void AnEliteOfEpicRarity_WearsOneModifierAlmostAlwaysAndFourAlmostNever()
        {
            var counts = ModifierCounts(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed));

            double atLeastOne = Share(counts, count => count >= 1);
            double allFour = Share(counts, count => count == NpcTypeDefaults.ModifierCount(CalibrationType, CalibrationRarity));

            Assert.IsTrue(atLeastOne >= 0.85, $"an epic elite came out bare too often: P(>=1) = {atLeastOne:P1}");
            Assert.IsTrue(allFour is >= 0.02 and <= 0.045,
                $"P(all four slots) = {allFour:P2}; the agreed band is 2..7% and a flat tail reaches 5.8%, so this holds it to 2..4.5%");
        }

        /// <summary>The middle of the ladder, which the endpoints do not pin: ONE modifier has to be the
        /// ordinary outcome of an epic elite, not merely a likely one. Two ways of reading the same slot
        /// chances agree on the top and the bottom of the distribution and disagree here — offering every
        /// slot its own independent roll instead of stopping at the first refusal keeps P(>=1) and P(4)
        /// where they are and drops this to about 28%, which is why the mass in the middle gets a line of
        /// its own.</summary>
        [TestMethod]
        public void OneModifierIsTheOrdinaryOutcomeForAnEpicElite_NotJustALikelyOne()
        {
            var counts = ModifierCounts(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed));

            double exactlyOne = Share(counts, count => count == 1);

            Assert.IsTrue(exactlyOne is >= 0.50 and <= 0.58,
                $"P(exactly one modifier) = {exactlyOne:P1}, outside the 50..58% that says the walk stops at the first refusal");
        }

        /// <summary>Rarity is the lever on the whole ladder, not decoration: the same elite record spawns
        /// far barer at uncommon than at epic. Goes red the moment the rarity multiplier is ignored.</summary>
        [TestMethod]
        public void RarityLiftsTheWholeLadder_AnUncommonEliteIsBarerThanAnEpicOne()
        {
            double uncommon = Share(ModifierCounts(CountRollingProvider(CalibrationType, Rarity.Uncommon, CalibrationSeed)), count => count >= 1);
            double epic = Share(ModifierCounts(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed)), count => count >= 1);

            Assert.IsTrue(uncommon <= 0.65, $"an uncommon elite wears a modifier {uncommon:P1} of the time — rarity is not being read");
            Assert.IsTrue(epic - uncommon >= 0.2, $"rarity barely moved the ladder: {uncommon:P1} at uncommon against {epic:P1} at epic");
        }

        /// <summary>Same seed, same NPCs. The counts are rolled on the generator the provider already
        /// spends on stance, level and rarity, so a save or a simulation replaying a seed must replay the
        /// modifier and ability counts with it.</summary>
        [TestMethod]
        public void TheSameSeedRollsTheSameCountsTwice()
        {
            var first = SpawnShape(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed), RollSamples);
            var second = SpawnShape(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed), RollSamples);
            var other = SpawnShape(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed + 1), RollSamples);

            CollectionAssert.AreEqual(first, second, "one seed handed out two different runs of NPCs");
            CollectionAssert.AreNotEqual(first, other, "two seeds handed out the same run, so the seed is not reaching the count roll");
        }

        /// <summary>The ability half of the same mechanic, at the two points the owner named: on a rank
        /// and file NPC a cast is an event, on an elite one or two of them is the norm.</summary>
        [TestMethod]
        public void OnARegularNpcACastIsAnEvent_OnAnEliteOneOrTwoIsTheNorm()
        {
            var regular = AbilityCounts(CountRollingProvider(EntityType.Regular, Rarity.Uncommon, CalibrationSeed));
            var elite = AbilityCounts(CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed));

            Assert.IsTrue(Share(regular, count => count == 0) >= 0.8,
                $"a regular NPC carries an ability {Share(regular, count => count > 0):P1} of the time — that is not an event");
            Assert.IsTrue(Share(elite, count => count >= 1) >= 0.85, "an epic elite came out with no ability far too often");
            Assert.IsTrue(Share(elite, count => count is 1 or 2) >= 0.6,
                $"one or two abilities is not the norm on an epic elite: {Share(elite, count => count is 1 or 2):P1}");
        }

        /// <summary>Abilities read their OWN ladder. The two tables share a shape and an epic elite has as
        /// many ability slots as modifier slots, so nothing but the numbers tells them apart: filling every
        /// ability slot is more than twice as likely as filling every modifier slot, and this goes red the
        /// moment the ability roll starts asking the modifier table.</summary>
        [TestMethod]
        public void TheAbilityLadderIsItsOwn_NotTheModifierOne()
        {
            var provider = CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed);
            var definitions = Spawns(provider);

            double allAbilitySlots = Share(definitions, definition => definition.Abilities.Count == NpcTypeDefaults.DefaultAbilityCount(CalibrationType));

            Assert.IsTrue(allAbilitySlots is >= 0.05 and <= 0.085,
                $"P(every ability slot filled) = {allAbilitySlots:P2}; the modifier ladder answers about 3% here, so this is which table was read");
        }

        /// <summary>Zero reads the same on both counts now: a record naming "abilityCount": 0 has answered,
        /// and the answer is none — not "I forgot to say, use the type". That is the training dummy, which
        /// is meant to stand there and be hit. The precondition is the whole point: the very same record
        /// with the field absent does get casts from the ladder.</summary>
        [TestMethod]
        public void AuthoredAbilityCountOfZero_MeansNoCastsAtAll_NotFallBackToTheType()
        {
            var rolling = CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed);
            var silent = CountRollingProvider(CalibrationType, CalibrationRarity, CalibrationSeed, 0);

            Assert.IsTrue(AbilityCounts(rolling, RollSamples).Any(count => count > 0),
                "precondition: this record does learn casts while the field is absent");
            Assert.IsTrue(AbilityCounts(silent, RollSamples).All(count => count == 0),
                "an authored count of zero must mean no casts, the way an authored modifier count of zero means no modifiers");
        }

        /// <summary>A record naming "abilityCount" is authoring, and authoring is absolute: the slot dice
        /// of a Regular would refuse almost every one of these, and they never get asked.</summary>
        [TestMethod]
        public void AuthoredAbilityCount_IsAbsolute_TheSlotDiceNeverTrimIt()
        {
            var provider = CountRollingProvider(EntityType.Regular, Rarity.Uncommon, CalibrationSeed, AuthoredAbilities);

            var counts = AbilityCounts(provider, RollSamples);

            Assert.IsTrue(counts.All(count => count == AuthoredAbilities),
                $"an authored count of {AuthoredAbilities} came out as {string.Join(", ", counts.Distinct())}");
        }

        /// <summary>The boss promise: his slot count is the whole stance pool
        /// (<see cref="NpcTypeDefaults.AllAbilities"/>), which is a statement and not a number to roll
        /// against — a boss must never lose casts to the slot dice.</summary>
        [TestMethod]
        public void ABossKeepsTheWholeStancePool_TheSlotDiceNeverTrimIt()
        {
            var provider = CountRollingProvider(EntityType.Boss, Rarity.Mythic, CalibrationSeed);

            var counts = AbilityCounts(provider, RollSamples);

            Assert.AreEqual(NpcTypeDefaults.AllAbilities, NpcTypeDefaults.DefaultAbilityCount(EntityType.Boss),
                "this test proves nothing once a boss stops taking the whole pool by default");
            Assert.IsTrue(counts.All(count => count == AbilityPoolSize),
                $"a boss came out with {counts.Min()} of the {AbilityPoolSize} abilities his pool offers");
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

        /// <summary>The provider is built by a container, and its constructor now ends in a seed the
        /// container has no service for — a parameter of that shape is exactly the kind that resolves in
        /// a test and throws at boot. This builds it the way a project's composition root does.</summary>
        [TestMethod]
        public void TheContainerCanStillBuildTheProvider_SeedAndAll()
        {
            var services = new ServiceCollection();
            services.AddSingleton(Mock.Of<IAbilityProvider>());
            services.AddSingleton(Mock.Of<INpcModifierProvider>());
            services.AddGameDataParticipant<INpcSpawnRollsProvider, NpcSpawnRollsProvider>();
            services.AddGameDataParticipant<INpcProvider, NpcProvider>();

            Assert.IsNotNull(services.BuildServiceProvider().GetRequiredService<INpcProvider>());
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

        /// <summary>A provider rolling the SLOT COUNT for real: the ladders the game ships, a seeded
        /// generator, and pools deep enough that neither count is ever cut short by an empty catalog.
        /// <paramref name="authoredAbilityCount"/> above zero puts the record's own "abilityCount" on it.</summary>
        private static NpcProvider CountRollingProvider(EntityType type, Rarity rarity, int seed, int? authoredAbilityCount = null)
        {
            var npc = Npc() with
            {
                EntityType = type.ToString(),
                Rarity = rarity.ToString(),
                AbilityCount = authoredAbilityCount,
            };

            var provider = CreateProvider(AbilityPoolIds(), ModifierPool(), ShippedSpawnRolls(), seed);
            provider.Apply(DataCatalog.Npc, NpcFile(npc));
            provider.Apply(DataCatalog.NpcBehaviors, BehaviorFile(WideArchetype(nameof(Stance.Dexterity))));
            return provider;
        }

        /// <summary>The whole shipped world as the game boots it: the Npc and NpcBehaviors catalogs, the
        /// shipped slot ladders, every ability the records name, and a modifier pool deeper than any record
        /// asks for — so what a shipped NPC comes out with is decided by its record and nothing else.</summary>
        private static NpcProvider ShippedProvider()
        {
            var provider = CreateProvider(ShippedAbilityIds(), DeepModifierPool(), ShippedSpawnRolls(), CalibrationSeed);
            provider.Apply(DataCatalog.Npc, ShippedFile(DataCatalog.Npc, "Npc.json"));
            provider.Apply(DataCatalog.NpcBehaviors, ShippedFile(DataCatalog.NpcBehaviors, "NpcBehavior.json"));
            return provider;
        }

        /// <summary>More modifiers than the deepest shipped ceiling asks for, so a short result is always
        /// the count rule and never an exhausted catalog.</summary>
        private static List<INpcModifier> DeepModifierPool() =>
            [.. Enumerable.Range(0, DeepModifierPoolSize).Select(index => PoolModifier($"Modifier_{index}"))];

        /// <summary>The slot ladders the game ships, read through the participant that reads them at boot:
        /// the calibration below is then a claim about the shipped file and not about numbers a test made up.</summary>
        private static INpcSpawnRollsProvider ShippedSpawnRolls()
        {
            var provider = new NpcSpawnRollsProvider();
            provider.Apply(DataCatalog.NpcSpawnRolls, ShippedFile(DataCatalog.NpcSpawnRolls, "NpcSpawnRolls.json"));
            return provider;
        }

        private static List<NpcDefinition> Spawns(NpcProvider provider, int samples = StatisticSamples) =>
            [.. Enumerable.Range(0, samples).Select(_ => provider.CreateDefinition(TestNpcId))];

        private static List<int> ModifierCounts(NpcProvider provider, int samples = StatisticSamples) =>
            [.. Spawns(provider, samples).Select(definition => definition.Modifiers.Count)];

        private static List<int> AbilityCounts(NpcProvider provider, int samples = StatisticSamples) =>
            [.. Spawns(provider, samples).Select(definition => definition.Abilities.Count)];

        /// <summary>Everything a seeded run must repeat, in one comparable line per spawn.</summary>
        private static List<string> SpawnShape(NpcProvider provider, int samples) =>
            [.. Spawns(provider, samples).Select(definition => $"{definition.Level}/{definition.Modifiers.Count}/{definition.Abilities.Count}")];

        private static double Share<T>(IReadOnlyCollection<T> samples, Func<T, bool> predicate) =>
            samples.Count(predicate) / (double)samples.Count;

        private static IReadOnlyCollection<string> AbilityPoolIds() =>
            [.. Enumerable.Range(0, AbilityPoolSize).Select(index => $"{PoolAbility}_{index}")];

        /// <summary>An archetype offering <see cref="AbilityPoolSize"/> casts of equal weight.</summary>
        private static NpcBehaviorData WideArchetype(string stance) => new()
        {
            Id = $"Behavior_{stance}",
            Stance = stance,
            Abilities = [.. AbilityPoolIds().Select(id => new NpcAbilityBehaviorData { Id = id, Weight = 1f, Role = nameof(AbilityRole.Damage) })],
        };

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

        private static NpcProvider CreateProvider(
            IReadOnlyCollection<string> abilityIds,
            IReadOnlyList<INpcModifier>? modifierPool = null,
            INpcSpawnRollsProvider? spawnRolls = null,
            int? rollSeed = null)
        {
            var abilities = new Mock<IAbilityProvider>();
            abilities.SetupGet(provider => provider.KnownAbilityIds).Returns(abilityIds);
            abilities.Setup(provider => provider.CreateAbility(It.IsAny<string>())).Returns(() => Mock.Of<IAbility>());

            var modifiers = new Mock<INpcModifierProvider>();
            modifiers.Setup(provider => provider.GetAllModifiers()).Returns(modifierPool ?? []);
            modifiers.Setup(provider => provider.GetModifier(It.IsAny<string>())).Returns(() => Mock.Of<INpcModifier>());

            return new NpcProvider(abilities.Object, modifiers.Object, spawnRolls ?? EverySlotFills(), rollSeed);
        }

        /// <summary>The ladder a test uses when it is not the subject: every slot the ceiling offers is
        /// taken, which is what the provider did before the slot dice existed and what it still does
        /// where no ladder is authored. Tests about anything but the count then read as they always did.</summary>
        private static INpcSpawnRollsProvider EverySlotFills()
        {
            var rolls = new Mock<INpcSpawnRollsProvider>();
            rolls.Setup(provider => provider.ModifierSlotChance(It.IsAny<EntityType>(), It.IsAny<Rarity>(), It.IsAny<int>())).Returns(1f);
            rolls.Setup(provider => provider.AbilitySlotChance(It.IsAny<EntityType>(), It.IsAny<Rarity>(), It.IsAny<int>())).Returns(1f);
            return rolls.Object;
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
