namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using Data.AbilityData;
    using Localization;

    /// <summary>
    /// The values an augment's description is printed with, assembled in one place for every surface that
    /// shows one — the copy in the bag, the copy in a socket, the record a ledger walks.
    /// <para>
    /// Two sources, in this order: the canon of the effect the augment lays, then whatever the record or
    /// the copy carries of its own, which wins on a shared key. That order is what a record laying an
    /// effect needs — it is forbidden to restate the effect's figures, so its own dictionary is empty and
    /// every number on its card belongs to the canon — while a record that does carry numbers still
    /// prints the ones it rolled.
    /// </para>
    /// </summary>
    public static class AugmentDescription
    {
        /// <param name="record">The augment as it is being shown: the bare record, or the record with a
        /// copy's own numbers and drawn effect already folded in (<see cref="AugmentInstance.Applied"/>).</param>
        /// <param name="effects">Where the canon is read. Null in a composition that has no effect
        /// registry, where a line keeps the placeholders it has rather than failing.</param>
        /// <param name="declared">Who answers for a record that lays an effect without naming it in data;
        /// see <see cref="LaidEffect"/>. Null where nobody does, which reads as "lays none".</param>
        public static Dictionary<string, object?> Values(
            AbilityAugmentData record,
            IEffectProvider? effects,
            IAugmentLaidEffects? declared = null) =>
            Values(record.UpgradeProperties, LaidEffect(record.Id, record.LaidEffectId, declared), effects);

        /// <param name="own">Numbers the record or the copy states itself.</param>
        /// <param name="laidEffectId">The effect the augment lays, or empty for one that lays none.</param>
        /// <param name="effects">Where the canon is read; see the overload above.</param>
        public static Dictionary<string, object?> Values(
            IReadOnlyDictionary<string, float> own,
            string laidEffectId,
            IEffectProvider? effects)
        {
            Dictionary<string, object?> printed = new(StringComparer.Ordinal);
            bool lays = !string.IsNullOrWhiteSpace(laidEffectId);

            if (lays && effects?.CanonOf(laidEffectId) is { } canon)
                foreach ((string key, float figure) in canon) printed[key] = figure;

            foreach ((string key, float figure) in own) printed[key] = figure;

            // What the augment lays, under a placeholder of its own: a pool record's line cannot be
            // written without it, and a record naming its effect outright is free to use it or not.
            if (lays) printed[AbilityAugmentData.EffectPlaceholder] = new LocalizedId(laidEffectId);

            return printed;
        }

        /// <summary>What an augment lays: its record's own word, and failing that the effect the factory
        /// building it was registered with. One rule in one place, so a card assembled on any road reaches
        /// the same canon — a record whose effect is named in code prints figures nobody restated.
        /// <para>The record's word comes first because a COPY of a pool record carries a drawn effect in
        /// exactly that field (<see cref="AugmentInstance.Applied"/>): what this particular copy lays has
        /// to beat anything said about the record in general.</para></summary>
        /// <param name="stated">What the record or the copy says itself; empty for a record that says
        /// nothing, which is the only case the declaration is consulted for.</param>
        public static string LaidEffect(string augmentId, string stated, IAugmentLaidEffects? declared) =>
            !string.IsNullOrWhiteSpace(stated) ? stated : declared?.LaidEffectOf(augmentId) ?? string.Empty;
    }
}
