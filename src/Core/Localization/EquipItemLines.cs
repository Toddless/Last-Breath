namespace Core.Localization
{
    using System.Collections.Generic;
    using System.Linq;
    using Items;
    using Modifiers;

    /// <summary>One display row of an equip item's line list. Parts of one composite roll (shared
    /// GroupId) join into a single row; <see cref="InstanceId"/> is the FIRST part's id — the reroll
    /// target contract (the reroll path removes the whole group by that one id). RevealedText is the
    /// Alt-hold variant with each part's roll spread appended ("+47 Strength (40–60)"); null when no
    /// part of the row carries a range, so the caller can fall back to Text without branching.</summary>
    public sealed record EquipItemLine(string Text, string? RevealedText, string InstanceId);

    /// <summary>Builds tooltip/crafting display rows from an item's modifier lists. Grouping and range
    /// decoration live here — in the formatter layer — so no UI ever touches GroupId/RolledRange stamps
    /// directly.</summary>
    public static class EquipItemLines
    {
        public static List<EquipItemLine> ComposeImplicits(IEquipItem item, TextFormat format = TextFormat.Plain) =>
            Compose(item.Implicits, item.ContextImplicits, format);

        public static List<EquipItemLine> ComposeRolled(IEquipItem item, TextFormat format = TextFormat.Plain) =>
            Compose(item.Modifiers, item.ContextModifiers, format);

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
                    Text: Localization.Format(modifier, format),
                    Range: Localization.FormatRolledRange(modifier, format)))
                .Concat(entries.Select(entry => (
                    entry.InstanceId,
                    entry.GroupId,
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

        private static EquipItemLine BuildLine(List<(string InstanceId, string? GroupId, string Text, string? Range)> parts)
        {
            string text = string.Join(", ", parts.Select(part => part.Text));
            string? revealed = parts.All(part => part.Range == null)
                ? null
                : string.Join(", ", parts.Select(part => part.Range == null ? part.Text : $"{part.Text} ({part.Range})"));
            return new EquipItemLine(text, revealed, parts[0].InstanceId);
        }
    }
}
