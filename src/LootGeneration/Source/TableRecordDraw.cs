namespace LootGeneration.Source
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Battle.Abilities;
    using Core.Data.AbilityData;
    using Core.Data.LootTable;
    using Core.Entity.Components;
    using Core.Enums;

    /// <summary>
    /// What a chosen table position turns out to be. A position naming an id already is the answer;
    /// a position naming a set is answered here, at the moment the drop is minted — the table stays
    /// the owner of WHAT drops, the draw only decides WHICH member of the set this kill got.
    /// <para>
    /// Late on purpose. Read at load, a group would freeze into the augments that existed then and
    /// the table would need editing whenever the catalog changed; read here, the set is whatever
    /// answers the filter at the moment the kill happens.
    /// </para>
    /// </summary>
    /// <param name="augments">What the augment ids of the game are. Held by every composition, which
    /// is why a drop can ask about augments without the module that fights.</param>
    public sealed class TableRecordDraw(IAbilityAugmentCatalog augments, IRandomNumberGenerator rnd)
    {
        /// <summary>What this position turned out to name; null when it names nothing — the kill goes
        /// on without it rather than losing the items around it.</summary>
        public DrawnDrop? Draw(TableRecord record) =>
            record.Augments is { } group ? DrawFromGroup(group) : new DrawnDrop(record.Id, null);

        /// <summary>One member of the set, uniformly, at the group's own rarity. Uniform because the
        /// members are alike by construction: the seat is priced once for all of them.</summary>
        private DrawnDrop? DrawFromGroup(AugmentGroup group)
        {
            // Ordered by id so a seeded run draws the same augment on any machine — the catalog's own
            // order follows the order its files happened to be read in.
            List<AbilityUpgradeData> members = augments.All
                .Where(record => record.Tier == group.Tier && Covers(record.RarityBand, group.Rarity))
                .OrderBy(record => record.Id, StringComparer.Ordinal)
                .ToList();

            if (members.Count > 0) return new DrawnDrop(members[rnd.RandIntRange(0, members.Count - 1)].Id, group.Rarity);

            // The budget for this seat is already spent, so the kill simply comes up one item short.
            // Reported rather than passed over: a group nobody answers is a table describing augments
            // the game does not have, and every kill rolling that seat pays for nothing.
            Tracker.TrackError($"Loot table group of tier {group.Tier} and rarity {group.Rarity} is answered by no augment: the position dropped nothing.", this);
            return null;
        }

        /// <summary>Whether a record may come out at the rarity the group is for. A record rolls a
        /// BAND, so membership is coverage rather than equality — an augment that can turn out Rare
        /// belongs in the Rare seat even though most of its copies do not. Note the scale runs
        /// downward: <see cref="Rarity.Legendary"/> is zero, so the best end is the smaller number.</summary>
        private static bool Covers((Rarity Worst, Rarity Best) band, Rarity rarity) =>
            (int)band.Best <= (int)rarity && (int)rarity <= (int)band.Worst;
    }

    /// <summary>
    /// What a bought position turned out to name. The rarity is the group's own and is carried to the
    /// mint rather than left to the copy's draw: the table is the owner of what it drops, so a seat
    /// bought as the Rare one produces a Rare copy of whichever member answered it. A position naming
    /// one thing by id decides no rarity — nothing about it says what the copy is worth — and the
    /// kind that mints it draws its own.
    /// </summary>
    public readonly record struct DrawnDrop(string Id, Rarity? Rarity);
}
