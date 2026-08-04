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
        /// <summary>The id to mint for this position; null when the position turns out to name
        /// nothing — the kill goes on without it rather than losing the items around it.</summary>
        public string? Draw(TableRecord record) => record.Augments is { } group ? DrawFromGroup(group) : record.Id;

        /// <summary>One member of the set, uniformly. Uniform because the members of a group share a
        /// tier and a rarity: they are worth the same, which is what let them be priced once.</summary>
        private string? DrawFromGroup(AugmentGroup group)
        {
            // Ordered by id so a seeded run draws the same augment on any machine — the catalog's own
            // order follows the order its files happened to be read in.
            List<AbilityUpgradeData> members = augments.All
                .Where(record => record.Tier == group.Tier && record.Rarity == group.Rarity)
                .OrderBy(record => record.Id, StringComparer.Ordinal)
                .ToList();

            if (members.Count > 0) return members[rnd.RandIntRange(0, members.Count - 1)].Id;

            // The budget for this seat is already spent, so the kill simply comes up one item short.
            // Reported rather than passed over: a group nobody answers is a table describing augments
            // the game does not have, and every kill rolling that seat pays for nothing.
            Tracker.TrackError($"Loot table group of tier {group.Tier} and rarity {group.Rarity} is answered by no augment: the position dropped nothing.", this);
            return null;
        }
    }
}
