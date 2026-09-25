namespace LastBreathTest.LootSimulation
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;
    using Core.Enums;

    /// <summary>
    /// The report is only worth reading if the same seed means the same run. These guard that promise
    /// from both sides: one process must repeat itself, and every process must agree with the runs
    /// recorded here.
    /// </summary>
    [TestClass]
    public class LootDeterminismTests
    {
        private const int Seed = 20260710;
        private const int Kills = 200;

        /// <summary>What <see cref="BossDrops"/> produced at <see cref="Seed"/> when the committed report
        /// was last regenerated. Recorded fingerprints move only when the drops move: regenerate the report
        /// (<c>LOOT_SIMULATION_REPORT=1 dotnet test</c>), commit it, and paste the value the failure prints.</summary>
        private const string BossDropsFingerprint = "29566E2A16D2B21FBD0FAF4D989001612C1F9D635A4E3140C0F4E297AEC2012C";

        /// <summary>The same for <see cref="RolledModifiers"/>. Moved by issue #222 (tier upgrades stack,
        /// one unique per section, scaling exempt), by the #224 sim rework (the modifier roll mirrors
        /// NpcProvider.RollModifiers of #223 — weighted pick WITHOUT replacement instead of the old
        /// WITH-replacement sampling), and by the #224 balance data pass: the guaranteedItems difficulties
        /// aligned to 0.4 shrink the budget of every kill that rolls one, and Rarity_Upgrade_Huge now
        /// boosts the legendary channel instead of the epic one — different budget, different chances,
        /// different drops on the same dice. <see cref="BossDropsFingerprint"/> did NOT move any of these
        /// times, which is the check that only the modifier side changed.</summary>
        private const string RolledModifiersFingerprint = "732561A22669BB130820B103743C5D0D86C94BBD6E1EFE8F730EA7DA3402F8FC";

        /// <summary>The cascade spawn of issue #223 (ceiling + falling slot chances), pinned the same way:
        /// this is the path every non-authored spawn takes in the game now, so the report is only worth
        /// reading if it reproduces too. Moved by the #224 balance pass — the Elit baseBudget went 20 → 28
        /// on top of the same modifier-data changes that moved <see cref="RolledModifiersFingerprint"/>;
        /// the count cascade itself spends the same dice as before.</summary>
        private const string CascadeSpawnFingerprint = "417C5582B45066F4904D0C7600F30656729B49A181EF8279FB82B8034F3BDF59";

        /// <summary>The boss carries the paths a regular kill never reaches — equip affix rolls, grant
        /// rolls and augment seats — which is where a run stops repeating itself first.</summary>
        private static NpcArchetype BossDrops => ScenarioCatalog.RichBaseline with { Name = "Determinism_Boss" };

        /// <summary>Rolls its own modifiers, which the boss above does not: that draw is the one place the
        /// drop pipeline turns the modifier catalog's ORDER into a choice, so only a scenario with
        /// <see cref="NpcArchetype.RandomModifierCount"/> above zero can hold that order still.</summary>
        private static NpcArchetype RolledModifiers => new("Determinism_Stack", EntityType.Boss, Rarity.Mythic, 100, Fractions.Demon)
        {
            RandomModifierCount = 5,
        };

        /// <summary>Rolls the modifier COUNT too — the slot cascade the other two never enter: FillsSlot
        /// burns its own dice, so this is the only scenario that can catch the cascade drifting off the
        /// seeded stream (a ladder chance crossing 1.0/0.0, a reordered draw).</summary>
        private static NpcArchetype CascadeSpawn => new("Determinism_Cascade", EntityType.Elit, Rarity.Epic, 30, Fractions.Undead)
        {
            CascadeRolled = true,
        };

        [TestMethod]
        public async Task SameSeedRepeatsWithinOneProcess()
        {
            foreach (var archetype in (NpcArchetype[])[BossDrops, RolledModifiers, CascadeSpawn])
            {
                string first = await FingerprintAsync(archetype);
                string second = await FingerprintAsync(archetype);

                Assert.AreEqual(first, second,
                    $"{archetype.Name}: two pipelines built on the same seed produced different drops, so something " +
                    "outside the seeded generator (shared state, a clock, a fresh Guid) is deciding loot.");
            }
        }

        [TestMethod]
        public async Task TheDropSequenceIsPinnedAcrossProcesses()
        {
            string actual = await FingerprintAsync(BossDrops);

            Assert.AreEqual(BossDropsFingerprint, actual,
                "The seeded drop sequence no longer matches the recorded run. If the change was intended, " +
                $"regenerate the report and record the new fingerprint: {actual}");
        }

        [TestMethod]
        public async Task TheRolledModifierSetIsPinnedToTheCatalogOrder()
        {
            string actual = await FingerprintAsync(RolledModifiers);

            // Goes red when NpcModifierProvider.GetAllModifiers stops sorting: the weight bands are laid
            // out in enumeration order, so an unordered catalog aims the same roll at another modifier.
            Assert.AreEqual(RolledModifiersFingerprint, actual,
                "The modifiers a seeded kill rolls have changed. If the change was intended, " +
                $"regenerate the report and record the new fingerprint: {actual}");
        }

        [TestMethod]
        public async Task TheSpawnCascadeIsPinnedAcrossProcesses()
        {
            string actual = await FingerprintAsync(CascadeSpawn);

            Assert.AreEqual(CascadeSpawnFingerprint, actual,
                "The #223 spawn cascade no longer rolls the same counts on the same seed. If the change " +
                $"was intended, regenerate the report and record the new fingerprint: {actual}");
        }

        /// <summary>A scenario run reduced to what a kill rolled and what it dropped. It is a canary on a
        /// fresh seed, not a copy of the report: the report spends ONE generator across all its scenarios
        /// in order, so a change can move the report without moving this — and cannot move this quietly.</summary>
        private static async Task<string> FingerprintAsync(NpcArchetype archetype)
        {
            var pipeline = LootPipeline.Create(Seed);
            var result = await new LootSimulator(pipeline).RunAsync(archetype, Kills);

            var sb = new StringBuilder();
            foreach (var kill in result.KillRecords)
            {
                sb.Append(kill.Difficulty.ToString("R", CultureInfo.InvariantCulture)).Append('|');
                foreach (var drop in kill.Drops)
                    sb.Append(drop.ItemId).Append(':').Append(drop.Rarity).Append(':').Append(drop.Stack)
                        .Append(':').Append(drop.Tier).Append(':').Append(drop.IsGuaranteed)
                        .Append(':').Append(drop.IsEquip).Append(':').Append(drop.IsCurrency).Append(';');

                sb.Append('\n');
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        }
    }
}
