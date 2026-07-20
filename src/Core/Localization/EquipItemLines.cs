namespace Core.Localization
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Items;
    using Modifiers;

    /// <summary>One display row of an equip item's line list. Parts of one composite roll (shared
    /// GroupId) join into a single row; <see cref="InstanceId"/> is the FIRST part's id — the reroll
    /// target contract (the reroll path removes the whole group by that one id). RevealedText is the
    /// Alt-hold variant with each part's roll spread appended ("+47 Strength (40–60)"); null when no
    /// part of the row carries a range, so the caller can fall back to Text without branching.
    /// Affix is the row's slot family (composite parts all carry the root's stamp) — the UI groups by it.</summary>
    public sealed record EquipItemLine(string Text, string? RevealedText, string InstanceId, AffixKind Affix);

    /// <summary>Builds tooltip/crafting display rows from an item's modifier lists. Grouping, ordering and
    /// range decoration live here — in the formatter layer — so no UI ever touches GroupId/RolledRange
    /// stamps directly and both panels show the same lines in the same order.</summary>
    public static class EquipItemLines
    {
        public static List<EquipItemLine> ComposeImplicits(IEquipItem item, TextFormat format = TextFormat.Plain) =>
            Compose(item.Implicits, item.ContextImplicits, format);

        /// <summary>Rolled rows in DISPLAY order: prefixes, suffixes, the family-less leftovers (legacy saves,
        /// authored fodder), then the ascension gift last. The sort is stable, so inside a block the rows keep
        /// the item's own order — a rerolled line reappears in the slot it was rerolled from.</summary>
        public static List<EquipItemLine> ComposeRolled(IEquipItem item, TextFormat format = TextFormat.Plain) =>
            Compose(item.Modifiers, item.ContextModifiers, format)
                .OrderBy(line => DisplayRank(line.Affix))
                .ToList();

        /// <summary>Block order of the affix families. Deliberately not the enum order: the family-less
        /// leftovers sink below the two real families, and the mythic gift always closes the list.</summary>
        private static int DisplayRank(AffixKind affix) => affix switch
        {
            AffixKind.Prefix => 0,
            AffixKind.Suffix => 1,
            AffixKind.Mythic => 3,
            _ => 2,
        };

        private static List<EquipItemLine> Compose(
            IReadOnlyList<IModifierInstance> modifiers,
            IReadOnlyList<ContextModifierEntry> entries,
            TextFormat format)
        {
            // A composite may mix entity and context parts, so both channels flatten into one ordered
            // list before grouping; a group's row sits where its first part appears.
            var parts = modifiers
                .Select(modifier => (
                    modifier.InstanceId,
                    GroupId: (modifier as SimpleModifier)?.GroupId,
                    Affix: (modifier as SimpleModifier)?.Affix ?? AffixKind.None,
                    Text: Localization.Format(modifier, format),
                    Range: Localization.FormatRolledRange(modifier, format)))
                .Concat(entries.Select(entry => (
                    entry.InstanceId,
                    entry.GroupId,
                    entry.Affix,
                    Text: Localization.Format(entry, format),
                    Range: Localization.FormatRolledRange(entry, format))))
                .ToList();

            var lines = new List<EquipItemLine>();
            var emittedGroups = new HashSet<string>();
            foreach (var part in parts)
            {
                if (part.GroupId == null)
                {
                    lines.Add(BuildLine([part]));
                    continue;
                }

                if (!emittedGroups.Add(part.GroupId)) continue;
                lines.Add(BuildLine(parts.Where(candidate => candidate.GroupId == part.GroupId).ToList()));
            }

            return lines;
        }

        private static EquipItemLine BuildLine(List<(string InstanceId, string? GroupId, AffixKind Affix, string Text, string? Range)> parts)
        {
            string text = string.Join(", ", parts.Select(part => part.Text));
            string? revealed = parts.All(part => part.Range == null)
                ? null
                : string.Join(", ", parts.Select(part => part.Range == null ? part.Text : $"{part.Text} ({part.Range})"));
            return new EquipItemLine(text, revealed, parts[0].InstanceId, parts[0].Affix);
        }
    }
}
