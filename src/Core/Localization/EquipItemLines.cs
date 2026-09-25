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
    /// Affix is the row's slot family (composite parts all carry the root's stamp) — the UI groups by it.
    /// A row held up by a condition carries the clause naming it in both texts, so a panel that only prints
    /// what it is given still tells the player when the bonus counts.</summary>
    public sealed record EquipItemLine(string Text, string? RevealedText, string InstanceId, AffixKind Affix);

    /// <summary>Builds tooltip/crafting display rows from an item's modifier lists. Grouping, ordering,
    /// range decoration and the condition clause live here — in the formatter layer — so no UI ever touches
    /// GroupId/RolledRange/ConditionId stamps directly and both panels show the same lines in the same order.</summary>
    public static class EquipItemLines
    {
        /// <summary>Implicit rows are the SPECIAL authored lines only — the piece's plain base stats
        /// live in the typed base channel (<see cref="IEquipItem.BaseStats"/>) and render as the item's
        /// stat block, never here.</summary>
        public static List<EquipItemLine> ComposeImplicits(IEquipItem item, TextFormat format = TextFormat.Plain, float? previewValueScale = null) =>
            Compose(item.Implicits, item.ContextImplicits, format, previewValueScale);

        /// <summary>Rolled rows in DISPLAY order: prefixes, suffixes, the family-less leftovers (legacy saves,
        /// authored fodder), then the ascension gift last. The sort is stable, so inside a block the rows keep
        /// the item's own order — a rerolled line reappears in the slot it was rerolled from. Sharpening does
        /// not reach these lines, so there is no forecast to append.</summary>
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

        /// <summary>One rendered contribution to a row: an entity modifier or a context entry, already
        /// reduced to what the row needs — its text, its optional roll spread and the stamps that decide
        /// which row it belongs to. Condition is the catalog id of the predicate holding the part up; a
        /// context entry has no such stamp to give, so it comes in ungated.</summary>
        private sealed record LinePart(string InstanceId, string? GroupId, AffixKind Affix, string? Condition, string Text, string? Range);

        private static List<EquipItemLine> Compose(
            IReadOnlyList<IModifierInstance> modifiers,
            IReadOnlyList<ContextModifierEntry> entries,
            TextFormat format,
            float? previewValueScale = null)
        {
            // A composite may mix entity and context parts, so both channels flatten into one ordered
            // list before grouping; a group's row sits where its first part appears.
            var parts = modifiers
                .Select(modifier => new LinePart(
                    modifier.InstanceId,
                    (modifier as SimpleModifier)?.GroupId,
                    (modifier as SimpleModifier)?.Affix ?? AffixKind.None,
                    (modifier as SimpleModifier)?.ConditionId,
                    WithPreview(Localization.Format(modifier, format), modifier, previewValueScale, format),
                    Localization.FormatRolledRange(modifier, format)))
                .Concat(entries.Select(entry => new LinePart(
                    entry.InstanceId,
                    entry.GroupId,
                    entry.Affix,
                    Condition: null,
                    WithPreview(Localization.Format(entry, format), entry, previewValueScale, format),
                    Localization.FormatRolledRange(entry, format))))
                .ToList();

            var lines = new List<EquipItemLine>();
            var emittedGroups = new HashSet<string>();
            foreach (var part in parts)
            {
                if (part.GroupId == null)
                {
                    lines.Add(BuildLine([part], format));
                    continue;
                }

                if (!emittedGroups.Add(part.GroupId)) continue;
                lines.Add(BuildLine(parts.Where(candidate => candidate.GroupId == part.GroupId).ToList(), format));
            }

            return lines;
        }

        /// <summary>Each scaling part carries its own preview tail, so a composite row stays readable:
        /// "+16.1% incr. Health → 17% (+0.9%), +12.1% incr. Evade → 12.7% (+0.6%)".</summary>
        private static string WithPreview(string text, object part, float? previewValueScale, TextFormat format) =>
            previewValueScale is { } scale && Localization.FormatUpgradePreview(part, scale, format) is { } suffix
                ? $"{text} {suffix}"
                : text;

        /// <summary>The row's gate is read off its first part — the same part its id and family come from.
        /// Every part of one composite carries the root's condition (the materializer stamps it on each), and
        /// the entity channel flattens first, so the leading part is the one that can name it.</summary>
        private static EquipItemLine BuildLine(List<LinePart> parts, TextFormat format)
        {
            string? condition = parts[0].Condition;
            string text = Localization.WithCondition(string.Join(", ", parts.Select(part => part.Text)), condition, format);
            string? revealed = parts.All(part => part.Range == null)
                ? null
                : Localization.WithCondition(
                    string.Join(", ", parts.Select(part => part.Range == null ? part.Text : $"{part.Text} ({part.Range})")),
                    condition,
                    format);
            return new EquipItemLine(text, revealed, parts[0].InstanceId, parts[0].Affix);
        }
    }
}
