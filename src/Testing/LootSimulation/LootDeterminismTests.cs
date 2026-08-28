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

        /// <summary>The same for <see cref="RolledModifiers"/>. Moved by issue #222, which changed what five
        /// random modifiers are worth on the same seed in three ways: tier upgrades now STACK instead of the
        /// strongest one winning; a rolled set keeps only one unique modifier per catalog section, the
        /// stronger taking the slot; and the scaling section is exempt from that (uniqueScope "id"), so
        /// different scalers still pile up. <see cref="BossDropsFingerprint"/> did NOT move, which is the
        /// check that only the modifier side changed.</summary>
        private const string RolledModifiersFingerprint = "58A94C5E5AD4EA0759D61EA0970E1DDA9AAB119BDF9CD926BC22C09F1E148135";

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

        [TestMethod]
        public async Task SameSeedRepeatsWithinOneProcess()
        {
            foreach (var archetype in (NpcArchetype[])[BossDrops, RolledModifiers])
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
