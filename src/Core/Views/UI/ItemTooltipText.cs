namespace Core.Views.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Items;
    using Localization;

    /// <summary>One row of a piece's typed base channel: what the stat is called, and what it reads with
    /// the item's own local lines already folded in.</summary>
    public readonly record struct ItemStatRow(string Name, string Value);

    /// <summary>One granted behaviour of a piece: its name, and what it does where the composition can
    /// say — a host with no effect registry builds no effect to ask, and the body is then empty.</summary>
    public readonly record struct ItemGrantText(string Name, string Description);

    /// <summary>
    /// One item as a card, in words and nothing else: the parts a surface draws, already worded, ordered
    /// and gated. Every list is empty where the item has nothing to put in it, so a surface hides a
    /// section by asking whether it is empty rather than by knowing which items have one.
    /// </summary>
    /// <param name="Title">The item's name, with its sharpening level where it carries one.</param>
    /// <param name="Subtitle">Rarity and what the piece IS — the weapon's type and grip, or its slot.</param>
    /// <param name="Stats">The typed base channel, row by row.</param>
    /// <param name="Implicits">The special authored lines of the piece.</param>
    /// <param name="Modifiers">The rolled lines, in display order with their slot family on each.</param>
    /// <param name="Effects">What the piece grants beyond its lines.</param>
    /// <param name="Lore">The item's own description; empty where the wording holds none.</param>
    public readonly record struct ItemCardText(
        string Title,
        string Subtitle,
        IReadOnlyList<ItemStatRow> Stats,
        IReadOnlyList<EquipItemLine> Implicits,
        IReadOnlyList<EquipItemLine> Modifiers,
        IReadOnlyList<ItemGrantText> Effects,
        string Lore)
    {
        /// <summary>Everything under the title as one stretch of lines, section captions included — for a
        /// surface carrying a single text field rather than a control per part. A section the item has
        /// nothing for leaves no caption and no blank line behind it.</summary>
        public IReadOnlyList<string> Lines => ItemTooltipText.Flatten(this);
    }

    /// <summary>
    /// What an item's card SAYS, assembled once for every surface that shows one. The framed tooltip and
    /// the crafting bench each draw their own controls and each decided their own wording, which is how
    /// one item came to read two ways — a rarity worded in one and printed as its enum name in the other.
    /// <para>Text only, and no engine type anywhere in it: the answer is the same whether it is asked by a
    /// window, by a test or by an authoring tool that never opens a scene.</para>
    /// </summary>
    public static class ItemTooltipText
    {
        /// <summary>Caption of the block holding the piece's special authored lines.</summary>
        public const string ImplicitsKey = "UI_Item_Implicits";

        /// <summary>Caption of the block holding the rolled lines.</summary>
        public const string ModifiersKey = "UI_Item_Modifiers";

        /// <summary>Caption of the block holding what the piece grants.</summary>
        public const string EffectKey = "UI_Item_Effect";

        /// <summary>Caption of the prefix block inside the rolled lines.</summary>
        public const string PrefixesKey = "UI_Item_Prefixes";

        /// <summary>Caption of the suffix block inside the rolled lines.</summary>
        public const string SuffixesKey = "UI_Item_Suffixes";

        /// <summary>Caption of the ascension gift, which closes the rolled lines.</summary>
        public const string MythicKey = "UI_Item_Mythic";

        private const string UpgradeFormat = "{0} +{1}";

        private const string StatFormat = "{0}   {1}";

        /// <summary>The combat triple a weapon opens its stat block with. Read off the weapon rather than
        /// out of its typed base channel — the three are the weapon's own contract and every weapon has
        /// them, whatever else its data writes.</summary>
        private static readonly EntityParameter[] s_weaponStats =
            [EntityParameter.PhysicalDamage, EntityParameter.CriticalChance, EntityParameter.CriticalDamage];

        /// <summary>The whole card of one item. <paramref name="formats"/> decides which base stats read as
        /// percentages; without it every one of them reads as a plain number.</summary>
        public static ItemCardText Card(IItem item, IParameterFormatProvider? formats, TextFormat format = TextFormat.Plain)
        {
            var equip = item as IEquipItem;

            return new ItemCardText(
                Title(item),
                Subtitle(item),
                equip is null ? [] : Stats(equip, formats),
                equip is null ? [] : EquipItemLines.ComposeImplicits(equip, format),
                equip is null ? [] : EquipItemLines.ComposeRolled(equip, format),
                equip is null ? [] : Effects(equip),
                Lore(item));
        }

        /// <summary>The item's name, and the sharpening level after it where the piece carries one — a
        /// level of zero is the state every piece starts in and says nothing about this one.</summary>
        public static string Title(IItem item) =>
            item is IEquipItem { UpdateLevel: > 0 } sharpened
                ? Format(UpgradeFormat, item.DisplayName, sharpened.UpdateLevel)
                : item.DisplayName;

        /// <summary>What the item is, under its name: the rarity, then the actual weapon (type and grip)
        /// for one, the slot for the rest of the equipment, and nothing more for anything else.</summary>
        public static string Subtitle(IItem item) => item switch
        {
            IWeaponItem weapon => EquipItemText.WeaponSubtitle(Rarity(item), weapon.WeaponType, weapon.Handedness),
            IEquipItem equip => string.Join(
                EquipItemText.Separator, Rarity(item), Localization.Localize(equip.EquipmentPiece.ToString())),
            _ => Rarity(item),
        };

        /// <summary>The caption a block of rolled lines opens under, or null for the family that shows
        /// none — the leftovers of legacy saves and authored fodder sit under the suffixes as a bare
        /// tail, and the ascension gift is drawn as a card whose frame is the label.</summary>
        public static string? AffixHeaderKey(AffixKind affix) => affix switch
        {
            AffixKind.Prefix => PrefixesKey,
            AffixKind.Suffix => SuffixesKey,
            AffixKind.Mythic => MythicKey,
            _ => null,
        };

        /// <summary>The card as lines. Written here rather than on the record so the record stays a bag of
        /// worded parts: the order of the sections is the card's rule and belongs beside the assembly.</summary>
        internal static IReadOnlyList<string> Flatten(ItemCardText card)
        {
            List<string> lines = [];

            if (card.Subtitle.Length > 0) lines.Add(card.Subtitle);
            lines.AddRange(card.Stats.Select(stat => Format(StatFormat, stat.Name, stat.Value)));

            Section(lines, ImplicitsKey, card.Implicits.Select(line => line.Text));
            Rolled(lines, card.Modifiers);
            Section(lines, EffectKey, card.Effects.SelectMany(Granted));

            if (card.Lore.Length > 0) lines.Add(card.Lore);

            return lines;
        }

        /// <summary>The rolled block with a caption opening each slot family. The rows arrive already
        /// ordered, so a family is opened where its first row stands and never again.</summary>
        private static void Rolled(List<string> lines, IReadOnlyList<EquipItemLine> rolled)
        {
            if (rolled.Count == 0) return;

            lines.Add(Caption(ModifiersKey));

            AffixKind? block = null;
            foreach (EquipItemLine line in rolled)
            {
                if (line.Affix != block && AffixHeaderKey(line.Affix) is { } key) lines.Add(Caption(key));

                block = line.Affix;
                lines.Add(line.Text);
            }
        }

        /// <summary>A captioned block, or nothing at all when there is nothing to caption.</summary>
        private static void Section(List<string> lines, string key, IEnumerable<string> rows)
        {
            List<string> written = [.. rows.Where(row => row.Length > 0)];

            if (written.Count == 0) return;

            lines.Add(Caption(key));
            lines.AddRange(written);
        }

        /// <summary>One grant as the lines it takes: its name, and its own wording under it where the
        /// composition could build the thing granted and ask.</summary>
        private static IEnumerable<string> Granted(ItemGrantText grant) =>
            grant.Description.Length > 0 ? [grant.Name, grant.Description] : [grant.Name];

        private static string Caption(string key) => Localization.Localize(key).ToUpperInvariant();

        /// <summary>The piece's typed base channel: a weapon opens with its combat triple, and every piece
        /// then reads one row per typed stat with its own local lines folded in.</summary>
        private static IReadOnlyList<ItemStatRow> Stats(IEquipItem item, IParameterFormatProvider? formats)
        {
            List<ItemStatRow> rows = [];

            if (item is IWeaponItem weapon)
                foreach (EntityParameter parameter in s_weaponStats)
                    rows.Add(Stat(parameter, weapon.GetStatBreakdown(parameter), formats));

            foreach (EntityParameter parameter in item.BaseStats.Keys)
                rows.Add(Stat(parameter, item.GetBaseStatBreakdown(parameter), formats));

            return rows;
        }

        private static ItemStatRow Stat(
            EntityParameter parameter, (float Base, float LocalBonus) breakdown, IParameterFormatProvider? formats) =>
            new(Localization.Localize(parameter.ToString()),
                ParameterValueText.Format(formats, parameter, breakdown.Base + breakdown.LocalBonus));

        private static IReadOnlyList<ItemGrantText> Effects(IEquipItem item) =>
            [.. item.Grants.Select(grant => new ItemGrantText(Localization.Localize(grant.Id), grant.Description))];

        /// <summary>The item's own description, and nothing where the wording holds none — the provider
        /// echoes the key back on a miss, and a card printing "Ring_Of_Ash_Description" is a card printing
        /// the absence of one.</summary>
        private static string Lore(IItem item) =>
            Localization.TryLocalizeDescription(item.Id, out string lore) ? lore : string.Empty;

        private static string Rarity(IItem item) => Localization.Localize(item.Rarity.ToString());

        private static string Format(string format, object first, object second) =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture, format, first, second);
    }
}
