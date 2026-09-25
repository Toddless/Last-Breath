namespace Core.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Renders localized templates with named values. Syntax:
    /// <c>{Damage}</c> — value by name (numbers formatted with a dot, colored in Rich mode);
    /// <c>{Duration|turn|turns}</c> — number + plural word via gettext (ru picks its own forms);
    /// <c>{@Effect_Clumsiness}</c> — keyword link: the localized name, in Rich wrapped into a
    /// clickable [url=key] (KeywordLinks opens the tooltip window on click);
    /// <c>{0}</c>, <c>{1}</c> — positional values (legacy templates);
    /// <c>{{</c> / <c>}}</c> — literal braces.
    /// Unknown placeholders stay in the output verbatim — visible in UI means discoverable.
    /// </summary>
    public partial class TextTemplateEngine(ILocalizationProvider localization)
    {
        private const string LeftBraceMark = "￰";
        private const string RightBraceMark = "￱";
        private static readonly Regex s_placeholder = MyRegex();

        public string Render(string template, IReadOnlyDictionary<string, object?> values, TextFormat format)
        {
            if (string.IsNullOrWhiteSpace(template)) return string.Empty;

            string escaped = template.Replace("{{", LeftBraceMark).Replace("}}", RightBraceMark);
            string result = s_placeholder.Replace(escaped, match => RenderToken(match, values, format));
            return result.Replace(LeftBraceMark, "{").Replace(RightBraceMark, "}");
        }

        public static string FormatNumber(object value) => value switch
        {
            float f => f.ToString("0.##", CultureInfo.InvariantCulture),
            double d => d.ToString("0.##", CultureInfo.InvariantCulture),
            decimal m => m.ToString("0.##", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        private string RenderToken(Match match, IReadOnlyDictionary<string, object?> values, TextFormat format)
        {
            string token = match.Groups["token"].Value;
            if (token.StartsWith('@')) return RenderKeyword(token[1..], format);

            string[] parts = token.Split('|');
            string name = parts[0];
            // {name:%} — the value is a fraction in data (0.15), shown as "15%"
            bool asPercent = name.EndsWith(":%", StringComparison.Ordinal);
            if (asPercent) name = name[..^2];

            if (!values.TryGetValue(name, out object? value) || value == null)
                return match.Value; // keep the token — a visible "{X}" beats a silent hole

            if (parts.Length == 3) return RenderPlural(value, parts[1], parts[2], format);

            if (asPercent && IsNumeric(value))
                return StyleNumber(FormatNumber(Convert.ToSingle(value, CultureInfo.InvariantCulture) * 100f) + "%", format);

            return IsNumeric(value) ? StyleNumber(FormatNumber(value), format) : value.ToString() ?? string.Empty;
        }

        private string RenderKeyword(string key, TextFormat format)
        {
            string name = localization.Translate(key);
            if (format != TextFormat.Rich) return name;

            // A damage-type keyword wears its type's color; every other keyword keeps the shared accent.
            string color = DamageKeywords.TryParse(key, out var damageType)
                ? TextPalette.DamageColor(damageType)
                : TextPalette.Keyword;
            return $"[url={key}]{TextPalette.Colorize(name, color)}[/url]";
        }

        private string RenderPlural(object value, string singularKey, string pluralKey, TextFormat format)
        {
            int count = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            string word = localization.TranslatePlural(singularKey, pluralKey, count);
            return $"{StyleNumber(count.ToString(CultureInfo.InvariantCulture), format)} {word}";
        }

        private static string StyleNumber(string text, TextFormat format) =>
            format == TextFormat.Rich ? TextPalette.ColorizeNumber(text) : text;

        private static bool IsNumeric(object value) =>
            value is float or double or decimal or int or long or short or byte or uint or ulong or ushort or sbyte;

        [GeneratedRegex(@"\{(?<token>[^{}]+)\}", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }
}
