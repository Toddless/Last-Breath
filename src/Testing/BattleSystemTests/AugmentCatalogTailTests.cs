namespace LastBreathTest.BattleSystemTests
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Enums;

    /// <summary>
    /// The tail of the catalog that is data and nothing else: what an augment is WORTH (its rarity) and
    /// what it refuses to sit beside (its exclusion group). Both are single fields nobody would notice
    /// going wrong — a rarity nobody minted at, a group with one member that forbids nothing.
    /// </summary>
    [TestClass]
    public class AugmentCatalogTailTests
    {
        /// <summary>The groups the shipped data declares, written out. A group is a design decision about
        /// which two augments may not be worn together, so it is named here rather than counted.</summary>
        private static readonly Dictionary<string, string[]> s_exclusionGroups = new(StringComparer.Ordinal)
        {
            // The single empowered strike replaces the series, so it cannot live beside anything that
            // buys more attacks. The two counters share the group as a side effect of the mechanic
            // being per-group rather than pairwise — they are rivals on one key anyway.
            ["Attack_Count"] =
            [
                "Ability_Ip_Augment_Single_Empowered_Attack",
                "Augment_Additional_Amount_Attacks",
                "Augment_Additional_Lunges"
            ],

            // Bounces and "every target" are two answers to the same question: how far the jar reaches.
            ["Jar_Reach"] = ["Ability_JoP_Augment_Bouncing", "Ability_JoP_Augment_All_Targets"],
        };

        [TestMethod]
        public void EveryExclusionGroupTheDataDeclaresIsTheOneWrittenDown()
        {
            var declared = ShippedAbilityData.Augments().All
                .Where(record => !string.IsNullOrWhiteSpace(record.ExclusionGroup))
                .GroupBy(record => record.ExclusionGroup, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Select(record => record.Id).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

            CollectionAssert.AreEquivalent(s_exclusionGroups.Keys.ToArray(), declared.Keys.ToArray(),
                "the shipped data declares different exclusion groups than the table names");

            foreach ((string group, string[] members) in s_exclusionGroups)
                CollectionAssert.AreEquivalent(members.Order(StringComparer.Ordinal).ToArray(), declared[group],
                    $"exclusion group '{group}' holds different records than the table names");
        }

        [TestMethod]
        public void AGroupWithOneMemberForbidsNothingAndIsATypo()
        {
            var lonely = ShippedAbilityData.Augments().All
                .Where(record => !string.IsNullOrWhiteSpace(record.ExclusionGroup))
                .GroupBy(record => record.ExclusionGroup, StringComparer.Ordinal)
                .Where(group => group.Count() < 2)
                .Select(group => group.Key)
                .ToList();

            Assert.AreEqual(0, lonely.Count,
                $"exclusion groups with a single member — they refuse nothing and read as a misspelling: {string.Join(", ", lonely)}");
        }

        [TestMethod]
        public void TwoAugmentsOfOneGroupNeverShareAnAbility()
        {
            // The rule the data is written for, asked of the rule that actually seats augments.
            foreach ((string group, string[] members) in s_exclusionGroups)
            {
                AbilityUpgradeData second = Record(members[1]);
                var slot = new AbilitySocketPlacement("socket", second.AbilityId, second.Tier);

                // The ability wears the record's own tags, so nothing but the group can refuse it.
                Assert.AreEqual(AugmentFitResult.ExclusionGroupTaken,
                    AugmentFit.Check(slot, second.Tags, second, [group]),
                    $"'{members[1]}' sat down beside its own group '{group}'");
            }
        }

        [TestMethod]
        public void EveryRarityTheCatalogDeclaresIsOneTheGameKnows()
        {
            // The field decides what the minted augment item is worth, so a value outside the scale
            // would price a drop at something nothing else in the game uses.
            var unknown = ShippedAbilityData.Augments().All
                .SelectMany(record => new[] { (record.Id, record.RarityBand.Worst), (record.Id, record.RarityBand.Best) })
                .Where(end => !Enum.IsDefined(end.Item2))
                .Select(end => $"{end.Id}: {end.Item2}")
                .ToList();

            Assert.AreEqual(0, unknown.Count, $"augments priced at a rarity the scale does not have: {string.Join(", ", unknown)}");
        }

        [TestMethod]
        public void EveryAugmentSaysWhatItMayRollBetween()
        {
            // The band is what a copy is drawn from, and the field it falls back to is the plainest
            // rarity there is. A record that simply forgot to say would not fail — it would quietly
            // mint every copy Common, a rarity the design list does not use at all.
            var unmarked = ShippedAbilityData.Augments().All
                .Where(record => record.MinRarity == null || record.MaxRarity == null)
                .Select(record => record.Id)
                .ToList();

            Assert.AreEqual(0, unmarked.Count,
                $"augments that name no rarity band and would mint at the field's default: {string.Join(", ", unmarked)}");
        }

        [TestMethod]
        public void EveryBandRunsFromTheWorstEndToTheBestAndStaysOnTheOrderedScale()
        {
            // Two ways to write a band that no draw can answer. Swapped ends make the worst better
            // than the best, and the draw would silently read them the other way round. Ends off the
            // ordered run — Unique and Mythic sit at 10 and 11, apart from Legendary..Common — make
            // "uniform between them" mean a walk across values that are not rarities of augments.
            var broken = ShippedAbilityData.Augments().All
                .Where(record => !RunsDownward(record.RarityBand) || !OnTheOrderedScale(record.RarityBand))
                .Select(record => $"{record.Id}: {record.RarityBand.Worst}..{record.RarityBand.Best}")
                .ToList();

            Assert.AreEqual(0, broken.Count,
                $"rarity bands nothing can be drawn from: {string.Join(", ", broken)}");
        }

        // Smaller is better on this scale, so the worst end must be the LARGER number.
        private static bool RunsDownward((Rarity Worst, Rarity Best) band) => (int)band.Best <= (int)band.Worst;

        private static bool OnTheOrderedScale((Rarity Worst, Rarity Best) band) =>
            Ordered(band.Worst) && Ordered(band.Best);

        private static bool Ordered(Rarity rarity) => rarity is >= Rarity.Legendary and <= Rarity.Common;

        private static AbilityUpgradeData Record(string augmentId)
        {
            AbilityUpgradeData? record = ShippedAbilityData.Augments().Find(augmentId);
            Assert.IsNotNull(record, $"the shipped data declares no '{augmentId}'");
            return record;
        }
    }
}
