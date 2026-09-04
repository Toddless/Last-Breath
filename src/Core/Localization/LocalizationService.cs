namespace Core.Localization
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Modifiers;

    public class LocalizationService(
        ILocalizationProvider provider,
        ModifierFormatter modifierFormatter,
        ContextModifierFormatter contextModifierFormatter,
        IEnumerable<ITextFormatter> formatters) : ILocalizationService
    {
        private readonly TextTemplateEngine _engine = new(provider);
        private readonly List<ITextFormatter> _formatters = formatters.ToList();

        /// <summary>What a thing's rule text is filed under: its id with this after it. The convention is
        /// spelled once so a reader holding only an <see cref="ILocalizationProvider"/> — a tree popup, an
        /// authoring tool — asks for the same key this service would.</summary>
        public const string DescriptionSuffix = "_Description";

        /// <summary>What the keyword CARD of a thing is filed under: its id with this after it. Separate
        /// from the rule text because the two are read in different places — the card is the standing
        /// wording a keyword link opens, and it falls back to the description when nothing is written.</summary>
        public const string TooltipSuffix = "_Tooltip";

        public ILocalizationProvider Provider => provider;

        public string Localize(string key) => provider.Translate(key);

        public string LocalizeDescription(string id) => provider.Translate(id + DescriptionSuffix);

        public string RenderDescription(string id, IReadOnlyDictionary<string, object?> values, TextFormat format = TextFormat.Plain) =>
            _engine.Render(LocalizeDescription(id), values, format);

        public string Render(string key, IReadOnlyDictionary<string, object?> values, TextFormat format = TextFormat.Plain) =>
            _engine.Render(provider.Translate(key), values, format);

        public string Format(object value, TextFormat format = TextFormat.Plain) =>
            _formatters.FirstOrDefault(formatter => formatter.CanFormat(value))?.Format(value, format) ?? string.Empty;

        public string FormatModifier(IModifier modifier, float rangeMinValue, float rangeMaxValue, TextFormat format = TextFormat.Plain) =>
            modifierFormatter.FormatRanged(modifier, rangeMinValue, rangeMaxValue, format);

        public string? FormatRolledRange(object line, TextFormat format = TextFormat.Plain) => line switch
        {
            IModifier modifier => modifierFormatter.FormatRolledRange(modifier, format),
            ContextModifierEntry entry => contextModifierFormatter.FormatRolledRange(entry, format),
            _ => null,
        };

        public string FormatParameterChange(EntityParameter parameter, float value, OperationType operation, TextFormat format = TextFormat.Plain) =>
            modifierFormatter.FormatParameterChange(parameter, value, operation, format);

        public string? FormatUpgradePreview(object line, float valueScale, TextFormat format = TextFormat.Plain) => line switch
        {
            IModifier modifier => PreviewSuffix(
                modifierFormatter.FormatValue(modifier.ModifierValueType, modifier.EntityParameter, modifier.Value * valueScale),
                modifierFormatter.FormatValue(modifier.ModifierValueType, modifier.EntityParameter, modifier.Value * (valueScale - 1f)),
                format),
            ContextModifierEntry { ValueType: not ModifierValueType.Flag } entry => PreviewSuffix(
                contextModifierFormatter.FormatValueOnly(entry, valueScale),
                contextModifierFormatter.FormatValueOnly(entry, valueScale - 1f),
                format),
            _ => null,
        };

        private static string PreviewSuffix(string after, string delta, TextFormat format) =>
            format == TextFormat.Rich
                ? $"→ {TextPalette.Colorize(after, TextPalette.Number)} {TextPalette.Colorize($"(+{delta})", TextPalette.Heal)}"
                : $"→ {after} (+{delta})";
    }
}
