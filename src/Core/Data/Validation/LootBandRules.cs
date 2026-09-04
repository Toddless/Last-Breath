namespace Core.Data.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AbilityData;
    using Enums;
    using GameData;
    using LootTable;
    using Newtonsoft.Json;

    /// <summary>The tiers of one loot table, under the name the run read it by. The positions are what the
    /// game's own reader made of the file: a seat it refused is not one the drop pipeline will ever
    /// price.</summary>
    public sealed record LootTableSeats(string Table, IReadOnlyList<LootTableTierData> Tiers)
    {
        /// <summary>
        /// The tables one file writes, read by the game's own converter. All four sections are taken as
        /// written rather than folded into the lookups a kill asks through, because a table is being looked
        /// at and not looked up — and a section a file leaves out is a section it has no tables in.
        /// </summary>
        /// <remarks>One reader for everyone asking: the caller brings the bytes — a document open in a tool
        /// or a shipped file — and what a table IS must not be answered two ways.</remarks>
        public static IEnumerable<LootTableSeats> From(string json)
        {
            if (JsonConvert.DeserializeObject<TablesData>(json) is not { } written) return [];

            return Sections(written).Select(table => new LootTableSeats(table.Key, table.Tiers));
        }

        private static IEnumerable<LootTableData> Sections(TablesData written) =>
            [.. written.General ?? [], .. written.Fractions ?? [], .. written.Types ?? [], .. written.Individual ?? []];
    }

    /// <summary>
    /// A loot position naming a set of augments is expanded when the item is MINTED and not at load, so a
    /// filter no augment answers is no load error at all — it is a kill that takes the seat's price out of
    /// the budget and quietly drops one item fewer, forever.
    /// <para>Membership is coverage and not equality: a record declares a band of rarities and belongs to
    /// every seat inside it. The scale runs downward — Legendary is zero — so the best end of a band is
    /// the smaller number, which is exactly the reading no filter written out by hand should have to
    /// repeat.</para>
    /// </summary>
    public static class LootBandRules
    {
        private const string UnansweredFormat =
            "no shipped augment declares a band covering tier {0} at {1}: the set is drawn when the item is "
            + "minted, so the seat takes its price out of the budget and hands nothing back.";

        private const string BandFormat = "tier {0} / {1}";

        private const string WhereFormat = "{0}, tier {1}";

        /// <summary>Every filter the tables write that no augment answers, said once for the filter and at
        /// the first seat writing it: the same band written at ten seats is one thing to fix, and ten rows
        /// saying it is a list nobody reads.</summary>
        public static IReadOnlyList<DataFinding> Check(
            IReadOnlyList<LootTableSeats> tables, IReadOnlyList<AbilityAugmentData> augments)
        {
            ArgumentNullException.ThrowIfNull(tables);
            ArgumentNullException.ThrowIfNull(augments);

            HashSet<AugmentGroup> said = [];
            List<DataFinding> found = [];

            foreach (LootTableSeats table in tables)
                foreach (LootTableTierData tier in table.Tiers)
                    foreach (TableRecord position in tier.Items)
                    {
                        if (position.Augments is not { } group || !said.Add(group)) continue;
                        if (Answered(group, augments)) continue;

                        found.Add(new DataFinding(
                            DataFindingKind.UnansweredAugmentBand,
                            DataCatalog.LootTables,
                            Record: string.Empty,
                            string.Format(BandFormat, group.Tier, group.Rarity),
                            string.Format(WhereFormat, table.Table, tier.Tier),
                            string.Format(UnansweredFormat, group.Tier, group.Rarity)));
                    }

            return found;
        }

        /// <summary>Whether any record answers the filter: the same tier, and a band the filter's rarity
        /// falls inside.</summary>
        private static bool Answered(AugmentGroup group, IReadOnlyList<AbilityAugmentData> augments)
        {
            foreach (AbilityAugmentData augment in augments)
                if (augment.Tier == group.Tier && Covers(augment.RarityBand, group.Rarity))
                    return true;

            return false;
        }

        /// <summary>Whether a band contains one rarity. Worst end first and the scale running downward, so
        /// the band is everything from the smaller number up to the larger one.</summary>
        private static bool Covers((Rarity Worst, Rarity Best) band, Rarity rarity) =>
            (int)band.Best <= (int)rarity && (int)rarity <= (int)band.Worst;
    }
}
