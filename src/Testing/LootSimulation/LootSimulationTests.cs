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
            var creation = new LootGeneration.Internal.ItemCreationService(
                new LootGeneration.Services.ItemEffectProvider(), s_pipeline.ItemProvider, s_pipeline.Rnd);

            var table = await s_pipeline.GetCombinedTableAsync(ScenarioCatalog.Baseline);
            var equipIds = table.Values.SelectMany(records => records)
                .Select(record => record.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .Where(id => TryCopy(id) is Core.Items.IEquipItem { Rarity: <= Rarity.Common })
                .Take(10)
                .ToList();
            Assert.IsTrue(equipIds.Count > 0, "No rollable equip items found in the baseline loot table.");

            foreach (string id in equipIds)
            foreach (float multiplier in (float[])[1f, 2f])
            {
                var poolValues = s_pipeline.ItemProvider.GetEquipItemModifierPool(id)
                    .Concat(s_pipeline.ItemProvider.GetEquipItemBaseModifierPool(id))
                    .SelectMany(ExpandLeaves)
                    .ToLookup(leaf => (leaf.EntityParameter, leaf.ModifierValueType), leaf => leaf.BaseValue);

                var generated = (Core.Items.IEquipItem)creation.CreateItem(id, [], Rarity.Rare, 0f, multiplier);
                foreach (var modifier in generated.Modifiers)
                {
                    bool matchesPool = poolValues[(modifier.EntityParameter, modifier.ModifierValueType)]
                        .Any(baseValue => Math.Abs(baseValue * multiplier - modifier.Value) <= 0.001f * Math.Max(1f, Math.Abs(modifier.Value)));
                    Assert.IsTrue(matchesPool,
                        $"{id} x{multiplier}: {modifier.EntityParameter}/{modifier.ModifierValueType} = {modifier.Value} matches no pool value.");
                }
            }
        }

        private static IEnumerable<IModifier> ExpandLeaves(IModifier modifier) =>
            modifier is CompositeModifier composite ? composite.Parts : [modifier];

        /// <summary>Loot tables reference ids that have no item data yet â€” those cannot be rolled.</summary>
        private static Core.Items.IItem? TryCopy(string id)
        {
            try
            {
                return s_pipeline.ItemProvider.CopyItem(id);
            }
            catch (ArgumentNullException)
            {
                return null;
            }
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
                int rolled = kill.Drops.Where(drop => !drop.IsGuaranteed).Sum(drop => drop.Stack);
                Assert.IsTrue(rolled <= cap, $"Rolled {rolled} items, cap is {cap}.");
            }
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
