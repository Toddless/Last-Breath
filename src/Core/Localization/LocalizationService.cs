namespace Core.Localization
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Modifiers;

    public class LocalizationService(
        ILocalizationProvider provider,
        ModifierFormatter modifierFormatter,
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

        public string FormatParameterChange(EntityParameter parameter, float value, OperationType operation, TextFormat format = TextFormat.Plain) =>
            modifierFormatter.FormatParameterChange(parameter, value, operation, format);
    }
}
