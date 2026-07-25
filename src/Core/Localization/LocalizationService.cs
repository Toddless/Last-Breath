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

        public ILocalizationProvider Provider => provider;

        public string Localize(string key) => provider.Translate(key);

        public string LocalizeDescription(string id) => provider.Translate(id + "_Description");

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
