namespace LastBreathTest.LootSimulation
{
    using Core.Enums;
    using Core.Modifiers;

    /// <summary>Fast, seeded invariants of the drop pipeline â€” the economy's regression net.
    /// Run only these with: dotnet test --filter "TestCategory!=Simulation"</summary>
    [TestClass]
    public class LootSimulationTests
    {
        private const int Seed = 20260710;
        private static LootPipeline s_pipeline = null!;
        private static LootSimulator s_simulator = null!;

        [ClassInitialize]
        public static void SetUp(TestContext _)
        {
            s_pipeline = LootPipeline.Create(Seed);
            s_simulator = new LootSimulator(s_pipeline);
        }

        [TestMethod]
        public async Task BaselineNpcAlwaysDropsWithinBudget()
        {
            var result = await s_simulator.RunAsync(ScenarioCatalog.Baseline, kills: 300);

            Assert.IsTrue(result.KillRecords.All(kill => kill.ItemCount > 0), "A baseline kill produced no loot.");
            foreach (var kill in result.KillRecords)
                Assert.IsTrue(kill.SpentValue <= kill.ExpectedBudget + 0.01f,
                    $"Spent {kill.SpentValue} exceeds the budget {kill.ExpectedBudget}.");
        }

        [TestMethod]
        public async Task MinRarityModifierEnforcesRarityFloor()
        {
            var archetype = ScenarioCatalog.RichBaseline with
            {
                Name = "Test_MinRarity",
                ModifierIds = ["Npc_Modifier_Min_Rarity_Epic"],
            };
            var result = await s_simulator.RunAsync(archetype, kills: 200);

            // Mythic/Unique templates keep their data rarity by design â€” the floor applies to rolled equips.
            var rolledEquips = result.KillRecords
                .SelectMany(kill => kill.Drops)
                .Where(drop => drop is { IsEquip: true, Rarity: <= Rarity.Common });
            foreach (var drop in rolledEquips)
                Assert.IsTrue(drop.Rarity <= Rarity.Epic, $"{drop.ItemId} dropped as {drop.Rarity}, below the Epic floor.");
        }

        [TestMethod]
        public async Task GuaranteedItemsDropOnEveryKill()
        {
            string[] expected =
            [
                "Crafting_Resource_Copper_Ore",
                "Crafting_Resource_Deer_Leather",
                "Crafting_Resource_Diamond_Gem",
                "Crafting_Resource_Linen_Fabric",
            ];
            var archetype = ScenarioCatalog.RichBaseline with
            {
                Name = "Test_Guaranteed",
                ModifierIds = ["Npc_Modifier_Guaranteed_Items_Crafting_Resource"],
            };
            var result = await s_simulator.RunAsync(archetype, kills: 100);

            foreach (var kill in result.KillRecords)
            {
                var droppedIds = kill.Drops.Select(drop => drop.ItemId).ToHashSet();
                foreach (string id in expected)
                    Assert.IsTrue(droppedIds.Contains(id), $"Guaranteed item {id} is missing from a kill.");
            }
        }

        [TestMethod]
        public async Task EquipStatsScaleLinearlyWithModifierMultiplier()
        {
            var creation = CreateItemCreation();
            var equipIds = await GetRollableEquipIdsAsync();

            foreach (string id in equipIds)
            foreach (float multiplier in (float[])[1f, 2f])
            {
                var poolValues = CombinedPool(id)
                    .SelectMany(descriptor => DescriptorOperations.Flatten([descriptor]))
                    .OfType<ParameterDescriptor>()
                    .ToLookup(leaf => (leaf.Parameter, leaf.ValueType), leaf => leaf.Value);

                var generated = (Core.Items.IEquipItem)creation.CreateItem(id, [], Rarity.Rare, 0f, multiplier);
                // Rolled lines are stamped with an affix; authored blueprint lines (Affix == None) come
                // from the item template, not the pool, and are out of this test's scope.
                // Pool entries carry roll spreads, so linearity means BOTH bounds scale with the
                // multiplier: the rolled value must land inside some entry's [Min, Max] × multiplier.
                foreach (var modifier in generated.Modifiers.OfType<SimpleModifier>().Where(line => line.Affix != AffixKind.None))
                {
                    float epsilon = 0.001f * Math.Max(1f, Math.Abs(modifier.Value));
                    bool matchesPool = poolValues[(modifier.EntityParameter, modifier.ModifierValueType)]
                        .Any(range => modifier.Value >= (range.Min * multiplier) - epsilon
                                      && modifier.Value <= (range.Max * multiplier) + epsilon);
                    Assert.IsTrue(matchesPool,
                        $"{id} x{multiplier}: {modifier.EntityParameter}/{modifier.ModifierValueType} = {modifier.Value} lands in no pool range.");
                }
            }
        }

        [TestMethod]
        public async Task GeneratedEquipCarriesExactlyTheAffixSlotsOfItsRarity()
        {
            var creation = CreateItemCreation();
            var equipIds = await GetRollableEquipIdsAsync();

            foreach (string id in equipIds)
            {
                var pool = CombinedPool(id);
                int prefixEntries = pool.Count(descriptor => descriptor.Affix == AffixKind.Prefix);
                int suffixEntries = pool.Count(descriptor => descriptor.Affix == AffixKind.Suffix);

                foreach (var rarity in (Rarity[])[Rarity.Common, Rarity.Uncommon, Rarity.Rare, Rarity.Epic, Rarity.Legendary])
                {
                    var generated = (Core.Items.IEquipItem)creation.CreateItem(id, [], rarity, 0f, 1f);
                    (int prefixLines, int suffixLines) = CountAffixLines(generated);

                    // A composite pick is ONE line (unit = GroupId); a thin bucket may under-fill its slots
                    // but can never overshoot, and a family never bleeds into the other's slots.
                    (int maxPrefixSlots, int maxSuffixSlots, int capacity) = rarity switch
                    {
                        Rarity.Uncommon => (1, 1, 1),
                        Rarity.Rare => (1, 1, 2),
                        Rarity.Epic => (2, 2, 3),
                        Rarity.Legendary => (2, 2, 4),
                        _ => (0, 0, 0),
                    };
                    Assert.IsTrue(prefixLines <= Math.Min(maxPrefixSlots, prefixEntries),
                        $"{id}/{rarity}: {prefixLines} prefix lines exceed min({maxPrefixSlots}, {prefixEntries}).");
                    Assert.IsTrue(suffixLines <= Math.Min(maxSuffixSlots, suffixEntries),
                        $"{id}/{rarity}: {suffixLines} suffix lines exceed min({maxSuffixSlots}, {suffixEntries}).");
                    Assert.IsTrue(prefixLines + suffixLines <= capacity,
                        $"{id}/{rarity}: {prefixLines + suffixLines} lines exceed the capacity {capacity}.");

                    // Fixed-split rarities fill exactly when the pool is rich enough.
                    if (rarity == Rarity.Rare)
                    {
                        Assert.AreEqual(Math.Min(1, prefixEntries), prefixLines, $"{id}/Rare under-filled its prefix slot.");
                        Assert.AreEqual(Math.Min(1, suffixEntries), suffixLines, $"{id}/Rare under-filled its suffix slot.");
                    }

                    if (rarity == Rarity.Legendary)
                    {
                        Assert.AreEqual(Math.Min(2, prefixEntries), prefixLines, $"{id}/Legendary under-filled its prefix slots.");
                        Assert.AreEqual(Math.Min(2, suffixEntries), suffixLines, $"{id}/Legendary under-filled its suffix slots.");
                    }
                }
            }
        }

        [TestMethod]
        public void UniqueAndMythicTemplatesGetNoAffixRolls()
        {
            var creation = CreateItemCreation();
            var templates = s_pipeline.ItemProvider.AllBlueprints
                .Where(blueprint => blueprint.Rarity is Rarity.Unique or Rarity.Mythic)
                .ToList();
            Assert.IsTrue(templates.Count > 0, "No Unique/Mythic templates in the data — the early-out is untestable.");

            foreach (var blueprint in templates)
            {
                var generated = (Core.Items.IEquipItem)creation.CreateItem(blueprint.Id, [], Rarity.Rare, 0f, 2f);

                Assert.AreEqual(blueprint.Rarity, generated.Rarity, $"{blueprint.Id}: generation re-rolled a template rarity.");
                (int prefixLines, int suffixLines) = CountAffixLines(generated);
                Assert.AreEqual(0, prefixLines + suffixLines, $"{blueprint.Id}: a fixed template received affix rolls.");
            }
        }

        private static LootGeneration.Internal.ItemCreationService CreateItemCreation() => new(
            s_pipeline.ItemProvider, s_pipeline.Rnd, s_pipeline.Minter, new ModifierMaterializer(s_pipeline.Rnd),
            new EmptyEffectCatalog(), new Core.Items.Grants.GrantFactory(() => null, () => null, () => null));

        private sealed class EmptyEffectCatalog : Core.Crafting.ICraftingEffectProvider
        {
            public IReadOnlyList<Core.Crafting.CraftingEffectOption> Effects => [];
        }

        private static async Task<List<string>> GetRollableEquipIdsAsync()
        {
            var table = await s_pipeline.GetCombinedTableAsync(ScenarioCatalog.Baseline);
            var equipIds = table.Values.SelectMany(records => records)
                .Select(record => record.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .Where(id => s_pipeline.ItemProvider.GetBlueprint(id) is { Rarity: <= Rarity.Common })
                .Take(10)
                .ToList();
            Assert.IsTrue(equipIds.Count > 0, "No rollable equip items found in the baseline loot table.");
            return equipIds;
        }

        private static List<IModifierDescriptor> CombinedPool(string id) =>
            s_pipeline.ItemProvider.GetEquipItemModifierPool(id)
                .Concat(s_pipeline.ItemProvider.GetEquipItemBaseModifierPool(id))
                .ToList();

        /// <summary>Rolled lines per family, counting a composite (shared GroupId) as ONE line across both channels.</summary>
        private static (int Prefixes, int Suffixes) CountAffixLines(Core.Items.IEquipItem item)
        {
            var units = item.Modifiers.OfType<SimpleModifier>()
                .Where(line => line.Affix != AffixKind.None)
                .Select(line => (line.Affix, Unit: line.GroupId ?? line.InstanceId))
                .Concat(item.ContextModifiers
                    .Where(entry => entry.Affix != AffixKind.None)
                    .Select(entry => (entry.Affix, Unit: entry.GroupId ?? entry.InstanceId)))
                .Distinct()
                .ToList();
            return (units.Count(unit => unit.Affix == AffixKind.Prefix), units.Count(unit => unit.Affix == AffixKind.Suffix));
        }

        [TestMethod]
        public async Task RolledItemCountRespectsConfiguredCap()
        {
            var archetype = new NpcArchetype("Test_Cap", EntityType.Archon, Rarity.Mythic, 150, Fractions.Demon)
            {
                ModifierIds =
                [
                    "Npc_Modifier_Scale_Double_Health",
                    "Npc_Modifier_Scale_Double_Damage",
                    "Npc_Modifier_Scale_Double_Defence",
                ],
            };
            var result = await s_simulator.RunAsync(archetype, kills: 100);

            int cap = s_pipeline.Configuration.MaxItemsPerKill;
            Assert.IsTrue(cap > 0, "MaxItemsPerKill is not configured.");
            foreach (var kill in result.KillRecords)
            {
                // The gold pile is leftover budget riding on top of the cap, not a rolled item.
                int rolled = kill.Drops.Where(drop => drop is { IsGuaranteed: false, IsCurrency: false }).Sum(drop => drop.Stack);
                Assert.IsTrue(rolled <= cap, $"Rolled {rolled} items, cap is {cap}.");
            }
        }

        [TestMethod]
        public async Task GoldDrop_RespectsCapAndPaysWeakKills()
        {
            // The anti-jackpot fuse: uncapped, an overfed archon minted ~46k gold in one kill
            // (sim 2026-07-24) against a ~500-gold trader restock — the cap must hold.
            var overfed = new NpcArchetype("Test_Gold_Cap", EntityType.Archon, Rarity.Mythic, 150, Fractions.Demon)
            {
                ModifierIds =
                [
                    "Npc_Modifier_Scale_Double_Health",
                    "Npc_Modifier_Scale_Double_Damage",
                    "Npc_Modifier_Scale_Double_Defence",
                ],
            };
            var capped = await s_simulator.RunAsync(overfed, kills: 50);

            int cap = s_pipeline.Configuration.MaxGoldPerKill;
            Assert.IsTrue(cap > 0, "MaxGoldPerKill is not configured.");
            foreach (var kill in capped.KillRecords)
                Assert.IsTrue(kill.GoldDropped <= cap, $"Minted {kill.GoldDropped} gold, cap is {cap}.");

            // The other end of the design: a weak kill whose budget affords no item still pays SOMETHING.
            var weak = new NpcArchetype("Test_Gold_Weak", EntityType.Regular, Rarity.Common, 1, Fractions.Undead);
            var weakResult = await s_simulator.RunAsync(weak, kills: 200);
            Assert.IsTrue(weakResult.MeanGoldPerKill > 0f, "weak kills must convert their budget into gold");
        }

        [TestMethod]
        public async Task TierUpgradeModifierRaisesTopTierShare()
        {
            var baseResult = await s_simulator.RunAsync(ScenarioCatalog.RichBaseline with { Name = "Test_TierBase" }, kills: 500);
            var upgraded = await s_simulator.RunAsync(
                ScenarioCatalog.RichBaseline with { Name = "Test_TierUp", ModifierIds = ["Npc_Modifier_Tier_Upgrade_To_Maximum"] },
                kills: 500);

            float baseShare = TopTierShare(baseResult);
            float upgradedShare = TopTierShare(upgraded);
            Assert.IsTrue(upgradedShare > baseShare,
                $"Tier upgrade modifier did not raise the tier-0 share: {baseShare} -> {upgradedShare}.");
            // The modifier procs on 20% of the purchases (tierUpgradeChance 0.2 in data): the share must
            // sit near that, not be a budget side-effect. Guards the once-dead JSON mapping.
            Assert.IsTrue(upgradedShare > 0.12f,
                $"Tier-0 share {upgradedShare:P1} is far below the 20% upgrade chance â€” the modifier looks dead again.");
        }

        private static float TopTierShare(ScenarioResult result)
        {
            int total = result.TierDistribution.Values.Sum();
            return total == 0 ? 0f : (float)result.TierDistribution.GetValueOrDefault(0) / total;
        }
    }
}
