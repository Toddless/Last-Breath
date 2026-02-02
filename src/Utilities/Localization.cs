namespace Utilities
{
    using Godot;
    using System;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Core.Modifiers;

    public static class Localization
    {
        private static readonly ModifierFormatter s_modifierFormatter;

        static Localization()
        {
            s_modifierFormatter = new ModifierFormatter(Localize);
        }

        public static string Format<T>(T obj)
        {
            switch (true)
            {
                case var _ when obj is IModifier modifier:
                    return s_modifierFormatter.FormatModifier(modifier);
            }

            return string.Empty;
        }

        public static string Localize(string id) => TranslationServer.Translate(id);
        public static string LocalizeDescription(string id) => TranslationServer.Translate(id + "_Description");
        public static string LocalizeDescriptionFormated(string id, params object[] args)
        {
            string template = LocalizeDescription(id);

            return ContainsNamedPlaceholder(template) ? FormatNamed(template, BuildNamedDictFromArgs(args)) : FormatNumbered(template, args);

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

            const string LMark = "\uFFF0";
            const string RMark = "\uFFF1";
            template = template.Replace("{{", LMark).Replace("}}", RMark);

            string res = Regex.Replace(template, @"\{(?<name>[^\}\{]+)\}", match =>
            {
                string name = match.Groups["name"].Value;
                if (int.TryParse(name, out _))
                    return match.Value;

                if (values.TryGetValue(name, out object? val))
                {
                    return val.ToString() ?? string.Empty;
                }

                GD.PrintErr($"Localization: missing named placeholder '{name}' for template '{template}'");
                return match.Value;
            });

            res = res.Replace(LMark, "{").Replace(RMark, "}");

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
            if (args.Length == 0) return dict;
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                if (args[i] is string key)
                    dict[key] = args[i + 1];
            }
            return dict;
        }
    }
}
