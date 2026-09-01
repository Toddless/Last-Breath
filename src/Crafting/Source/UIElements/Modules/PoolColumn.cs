namespace Crafting.Source.UIElements.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Enums;
    using Core.Localization;
    using Core.Modifiers;
    using Core.Views.UI;
    using Godot;
    using SharedUi;

    /// <summary>The "possible modifiers" column: sections per slot family with entry counts. The
    /// window computes which descriptors the current operation can roll; the column groups, orders
    /// and prints them, hiding itself when the pool is empty.</summary>
    [GlobalClass]
    public partial class PoolColumn : VBoxContainer
    {
        private const string HeaderKey = "UI_Craft_Pool";
        private const string CounterKey = "UI_Pool_Counter";
        private const string ExtraLevelsKey = "UI_Pool_Extra_Levels";
        private const string IncreaseKindKey = "UI_Mod_Kind_Increase";
        private const string MultiplicativeKindKey = "UI_Mod_Kind_Multiplicative";
        private const string FlatKindKey = "UI_Mod_Kind_Flat";

        [Export] private Label? _header;
        [Export] private Label? _counter;
        [Export] private VBoxContainer? _list;

        private ModifierFormatter? _formatter;

        public override void _Ready() => _header?.Text = Localization.Localize(HeaderKey).ToUpper();

        public void SetFormatter(ModifierFormatter? formatter) => _formatter = formatter;

        /// <summary>Repaints the pool. Split rows render parameter entries as "kind + name" with the
        /// gold roll interval; a tint colours sentence rows; the predicate mutes entries the current
        /// operation would refuse right now. An empty pool hides the column.</summary>
        public void SetPool(IEnumerable<IModifierDescriptor> descriptors, Color? tint, bool splitRows,
            Func<IModifierDescriptor, bool>? ineligible)
        {
            if (_list == null) return;
            _list.QueueFreeChildren();

            var rows = DescriptorSections(descriptors, tint, splitRows, out int entryCount, ineligible);
            Visible = rows.Count > 0;
            if (rows.Count == 0) return;

            _counter?.Text = Localization.Render(CounterKey, new Dictionary<string, object?> { ["Count"] = entryCount });
            foreach (var row in rows)
                _list.AddChild(row);
        }

        /// <summary>Pool rows grouped by slot family (prefixes / suffixes / tail / mythic), each block
        /// opened by the shared affix caption with its entry count. A parameter descriptor splits into
        /// "kind + name" and the gold roll interval; sentence-shaped entries (context, composites) and
        /// grants keep their single-line form. Duplicates (same visible row) collapse.</summary>
        private List<Control> DescriptorSections(IEnumerable<IModifierDescriptor> descriptors, Color? tint, bool splitRows, out int entryCount,
            Func<IModifierDescriptor, bool>? ineligible = null)
        {
            var rows = new List<Control>();
            entryCount = 0;
            foreach (var family in descriptors
                         .GroupBy(descriptor => descriptor.Affix)
                         .OrderBy(group => FamilyRank(group.Key)))
            {
                var familyRows = new List<Control>();
                var seen = new HashSet<string>();
                foreach (var descriptor in OrderForDisplay(family))
                {
                    var row = DescriptorRow(descriptor, tint, splitRows, seen, ineligible?.Invoke(descriptor) == true);
                    if (row != null) familyRows.Add(row);
                }

                if (familyRows.Count == 0) continue;
                entryCount += familyRows.Count;
                var header = ItemLineRows.AffixHeader(family.Key, familyRows.Count);
                if (header != null) rows.Add(header);
                rows.AddRange(familyRows);
            }

            return rows;
        }

        /// <summary>Same block order the item line lists use: prefixes, suffixes, the tail, mythic last.</summary>
        private static int FamilyRank(AffixKind affix) => affix switch
        {
            AffixKind.Prefix => 0,
            AffixKind.Suffix => 1,
            AffixKind.Mythic => 3,
            _ => 2,
        };

        /// <summary>Inside a family block the rollable lines group by their stat (shown name), flats
        /// before increases before multipliers, each run high to low — "+ PhysicalDamage" entries sit
        /// together from the biggest roll down. Sentence-shaped entries follow in authored order.</summary>
        private static IEnumerable<IModifierDescriptor> OrderForDisplay(IEnumerable<IModifierDescriptor> family)
        {
            var parameters = new List<ParameterDescriptor>();
            var sentences = new List<IModifierDescriptor>();
            foreach (var descriptor in family)
            {
                if (descriptor is ParameterDescriptor parameter) parameters.Add(parameter);
                else sentences.Add(descriptor);
            }

            return parameters
                .OrderBy(parameter => Localization.Localize(parameter.Parameter.ToString()))
                .ThenBy(parameter => KindRank(parameter.ValueType))
                .ThenByDescending(parameter => parameter.Value.Max)
                .ThenByDescending(parameter => parameter.Value.Min)
                .Concat(sentences);
        }

        private static int KindRank(ModifierValueType valueType) => valueType switch
        {
            ModifierValueType.Increase => 1,
            ModifierValueType.Multiplicative => 2,
            _ => 0,
        };

        private Control? DescriptorRow(IModifierDescriptor descriptor, Color? tint, bool splitRows, HashSet<string> seen, bool muted = false)
        {
            var mutedColor = Color.FromHtml(TextPalette.System);
            if (splitRows && descriptor is ParameterDescriptor parameter && _formatter != null)
            {
                string name = $"{Localization.Localize(KindKey(parameter.ValueType))} {Localization.Localize(parameter.Parameter.ToString())}";
                string range = _formatter.FormatDescriptorRange(parameter);
                return seen.Add($"{name}|{range}") ? SplitRow(name, range, muted ? mutedColor : tint, muted) : null;
            }

            string text = descriptor switch
            {
                GrantDescriptor grant => Localization.Localize(grant.GrantId),
                UpgradeLevelsDescriptor levels => Localization.Render(ExtraLevelsKey,
                    new Dictionary<string, object?> { ["Min"] = levels.Min, ["Max"] = levels.Max }),
                _ => Localization.Format(descriptor),
            };
            if (string.IsNullOrEmpty(text) || !seen.Add(text)) return null;

            var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
            if (muted) label.AddThemeColorOverride("font_color", mutedColor);
            else if (tint is { } color) label.AddThemeColorOverride("font_color", color);
            return label;
        }

        /// <summary>How a rollable line announces its shape: "+ Health", "+% incr. Evade", "+% more Armor".</summary>
        private static string KindKey(ModifierValueType valueType) => valueType switch
        {
            ModifierValueType.Increase => IncreaseKindKey,
            ModifierValueType.Multiplicative => MultiplicativeKindKey,
            _ => FlatKindKey,
        };

        private static Control SplitRow(string name, string value, Color? tint, bool muted = false)
        {
            var row = KeyValueRow.Initialize().Instantiate<KeyValueRow>();
            row.Set(name, value, Color.FromHtml(muted ? TextPalette.System : TextPalette.Number));
            row.SetPlainCaption(tint);
            row.EnableCaptionAutowrap();
            return row;
        }
    }
}
