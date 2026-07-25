namespace Core.Localization
{
    using System;
    using System.Globalization;
    using Enums;
    using Modifiers;

    /// <summary>
    /// Turns a context line into a localized phrase through per-knob .po templates:
    /// Context_Modifier_&lt;Parameter&gt; with {value}, and the _Range twin with {min}/{max} for
    /// unrolled spreads. Increase/Multiplicative render as percent, Flat as the floored whole
    /// value — matching how the bindings consume whole-number knobs.
    /// </summary>
    public class ContextModifierFormatter(ILocalizationProvider localization)
    {
        public string Format(ContextModifierEntry entry, TextFormat format = TextFormat.Plain)
        {
            // Templates are full sentences ("Increases ... by {value}"), so positive values carry no sign.
            string value = FormatValueOnly(entry);
            string styledValue = format == TextFormat.Rich ? TextPalette.ColorizeNumber(value) : value;
            return localization.Translate($"Context_Modifier_{entry.Parameter}").Replace("{value}", styledValue);
        }

        /// <summary>The entry's bare display value at an optional projected scale ("35%", "3") —
        /// the sharpening preview shows it next to the full sentence.</summary>
        public string FormatValueOnly(ContextModifierEntry entry, float valueScale = 1f) =>
            entry.ValueType == ModifierValueType.Flat
                ? Number((int)(entry.Value * valueScale))
                : Number(entry.Value * valueScale * 100f) + "%";

        /// <summary>An unmaterialized pool/blueprint line: a fixed value renders like a live entry,
        /// a spread renders through Context_Modifier_&lt;Parameter&gt;_Range with {min} and {max}.</summary>
        public string FormatDescriptor(ContextDescriptor descriptor, TextFormat format = TextFormat.Plain)
        {
            if (descriptor.Value.IsFixed)
                return Format(new ContextModifierEntry(descriptor.Parameter, descriptor.ValueType, descriptor.Value.Min), format);

            (string min, string max) = RangeParts(descriptor.ValueType, descriptor.Value.Min, descriptor.Value.Max, format);
            return localization.Translate($"Context_Modifier_{descriptor.Parameter}_Range")
                .Replace("{min}", min)
                .Replace("{max}", max);
        }

        /// <summary>The roll spread of a materialized line as a bare interval ("2–4", "35–50%") for the
        /// Alt reveal, scaled to the line's current value channel (see ModifierFormatter.FormatRolledRange).
        /// Null when the line was minted from a fixed value.</summary>
        public string? FormatRolledRange(ContextModifierEntry entry, TextFormat format = TextFormat.Plain)
        {
            if (entry.RolledRange is not { } range) return null;

            float levelScale = Math.Abs(entry.BaseValue) < 0.0001f ? 1f : entry.Value / entry.BaseValue;
            string text = entry.ValueType == ModifierValueType.Flat
                ? $"{Number((int)(range.Min * levelScale))}–{Number((int)(range.Max * levelScale))}"
                : $"{Number(range.Min * levelScale * 100f)}–{Number(range.Max * levelScale * 100f)}%";
            return format == TextFormat.Rich ? TextPalette.ColorizeNumber(text) : text;
        }

        /// <summary>{min} is a bare number, {max} carries the percent unit — full-sentence templates
        /// supply the wording ("Increases ... by 35–50%").</summary>
        private static (string Min, string Max) RangeParts(ModifierValueType valueType, float min, float max, TextFormat format)
        {
            (string minText, string maxText) = valueType == ModifierValueType.Flat
                ? (Number((int)min), Number((int)max))
                : (Number(min * 100f), Number(max * 100f) + "%");
            return format == TextFormat.Rich
                ? (TextPalette.ColorizeNumber(minText), TextPalette.ColorizeNumber(maxText))
                : (minText, maxText);
        }

        private static string Number(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
