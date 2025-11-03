namespace Utilities
{
    using Godot;
    using System;
    using Core.Interfaces;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public static partial class Localizator
    {
        private static ModifierFormatter s_modifierFormatter;

        static Localizator()
        {
            s_modifierFormatter = new ModifierFormatter(Localize);
        }

        public static string Format<T>(T obj)
        {
            switch (true)
            {
                case var _ when obj is IModifier modifier:
                    return s_modifierFormatter.FormatModifier(modifier);
                default:
                    break;
            }

            return string.Empty;
        }

        public static string Localize(string id) => TranslationServer.Translate(id);
        public static string LocalizeDescription(string id) => TranslationServer.Translate(id + "_Description");
        public static string LocalizeDescriptionFormated(string id, params object[] args)
        {
            var template = LocalizeDescription(id);

            return ContainsNamedPlaceholder(template) ? FormatNamed(template, BuildNamedDictFromArgs(args)) : FormatNumbered(template, args);

        }

        private static string LocalizeFormatNamed(string id, IDictionary<string, object> values)
        {
            var template = Localize(id);
            return FormatNamed(template, values);
        }

        private static string FormatNumbered(string template, object[] args)
        {
            if (string.IsNullOrWhiteSpace(template)) return string.Empty;
            try
            {
                return string.Format(CultureInfo.CurrentUICulture, template, args);
            }
            catch (Exception ex)
            {
                Tracker.TrackException($"Failed to format: {template}", ex);
                return template;
            }
        }

        private static string FormatNamed(string template, IDictionary<string, object> values)
        {
            if (string.IsNullOrWhiteSpace(template)) return string.Empty;
            values ??= new Dictionary<string, object>();

            const string L_MARK = "\uFFF0";
            const string R_MARK = "\uFFF1";
            template = template.Replace("{{", L_MARK).Replace("}}", R_MARK);

            var res = Regex.Replace(template, @"\{(?<name>[^\}\{]+)\}", match =>
            {
                var name = match.Groups["name"].Value;
                if (int.TryParse(name, out _))
                    return match.Value;

                if (values.TryGetValue(name, out var val))
                {
                    return val?.ToString() ?? string.Empty;
                }
                else
                {
                    GD.PrintErr($"Lokalizator: missing named placeholder '{name}' for template '{template}'");
                    return match.Value;
                }
            });

            res = res.Replace(L_MARK, "{").Replace(R_MARK, "}");

            return res;
        }

        private static bool ContainsNamedPlaceholder(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var match = Regex.Match(text, @"\{(?<name>[^\}\{]+)\}");
            if (!match.Success) return false;
            return !int.TryParse(match.Groups["name"].Value, out _);
        }

        private static IDictionary<string, object> BuildNamedDictFromArgs(object[] args)
        {
            var dict = new Dictionary<string, object>();
            if (args == null) return dict;
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                if (args[i] is string key)
                    dict[key] = args[i + 1];
            }
            return dict;
        }
    }
}
