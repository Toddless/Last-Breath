namespace Core.Localization
{
    using System.Globalization;
    using Enums;
    using Modifiers;

    /// <summary>
    /// Turns a context line into a localized phrase through per-knob .po templates:
    /// Context_Modifier_&lt;Parameter&gt; with {value}. Increase/Multiplicative render as percent,
    /// Flat as the floored whole value — matching how the bindings consume whole-number knobs.
    /// </summary>
    public class ContextModifierFormatter(ILocalizationProvider localization)
    {
        public string Format(ContextModifierEntry entry, TextFormat format = TextFormat.Plain)
        {
            // Templates are full sentences ("Increases ... by {value}"), so positive values carry no sign.
            string value = entry.ValueType == ModifierValueType.Flat
                ? Number(entry.WholeValue)
                : Number(entry.Value * 100f) + "%";
            string styledValue = format == TextFormat.Rich ? TextPalette.ColorizeNumber(value) : value;
            return localization.Translate($"Context_Modifier_{entry.Parameter}").Replace("{value}", styledValue);
        }

        private static string Number(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
