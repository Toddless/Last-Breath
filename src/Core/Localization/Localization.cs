namespace Core.Localization
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;

    /// <summary>
    /// Thin static facade over ILocalizationService for UI convenience. The real logic is
    /// DI-hosted (pure core + Godot provider); tests target the core classes directly or
    /// swap the service via <see cref="Override"/>.
    /// </summary>
    public static class Localization
    {
        private static ILocalizationService? s_service;

        private static ILocalizationService Service =>
            s_service ??= Services.GameServiceProvider.Instance.GetService<ILocalizationService>();

        /// <summary>Test/tooling hook: pin a service instead of resolving from the game container.</summary>
        public static void Override(ILocalizationService service) => s_service = service;

        public static string Localize(string id) => Service.Localize(id);

        public static string LocalizeDescription(string id) => Service.LocalizeDescription(id);

        /// <summary>False when the catalog has no description entry for the id. The provider echoes the key
        /// back on a miss, so a miss is told from a wording by rebuilding the key — under
        /// <see cref="LocalizationService.DescriptionSuffix"/>, which is where that convention is spelled.</summary>
        public static bool TryLocalizeDescription(string id, out string description)
        {
            description = Service.LocalizeDescription(id);
            return !string.IsNullOrEmpty(description) && description != id + LocalizationService.DescriptionSuffix;
        }

        public static string RenderDescription(string id, IReadOnlyDictionary<string, object?> values, TextFormat format = TextFormat.Plain) =>
            Service.RenderDescription(id, values, format);

        public static string Render(string key, IReadOnlyDictionary<string, object?> values, TextFormat format = TextFormat.Plain) =>
            Service.Render(key, values, format);

        public static string FormatParameterChange(EntityParameter parameter, float value, OperationType operation, TextFormat format = TextFormat.Plain) =>
            Service.FormatParameterChange(parameter, value, operation, format);

        public static string Format<T>(T obj) => obj == null ? string.Empty : Service.Format(obj);

        public static string Format<T>(T obj, TextFormat format) => obj == null ? string.Empty : Service.Format(obj, format);

        /// <summary>A rendered line joined to the clause of the condition holding it up; unchanged when the
        /// line names none.</summary>
        public static string WithCondition(string line, string? conditionId, TextFormat format = TextFormat.Plain) =>
            ConditionalLineText.Join(Service.Provider, line, conditionId, format);

        /// <summary>The roll spread of a materialized item line ("40–60") for the Alt reveal; null when fixed.</summary>
        public static string? FormatRolledRange(object line, TextFormat format = TextFormat.Plain) =>
            Service.FormatRolledRange(line, format);

        /// <summary>The sharpening preview tail of an item line ("→ 203.2 (+9.7)"); null for flags and unknown types.</summary>
        public static string? FormatUpgradePreview(object line, float valueScale, TextFormat format = TextFormat.Plain) =>
            Service.FormatUpgradePreview(line, valueScale, format);

        public static string Format<T>(T obj, float minValueMultiplier, float maxValueMultiplier) =>
            obj is IModifier modifier
                ? Service.FormatModifier(modifier, minValueMultiplier, maxValueMultiplier)
                : string.Empty;

        /// <summary>
        /// Legacy entry: positional args ({0}, {1}) or ("name", value) pairs. New code passes a
        /// values dictionary to <see cref="RenderDescription"/> instead.
        /// </summary>
        public static string LocalizeDescriptionFormated(string id, params object[] args) =>
            Service.RenderDescription(id, BuildValues(args));

        private static Dictionary<string, object?> BuildValues(object[] args)
        {
            var values = new Dictionary<string, object?>();
            if (args.Length == 0) return values;

            // ("name", value) pairs when the first arg is a string key, positional otherwise
            if (args[0] is string && args.Length % 2 == 0)
            {
                for (int i = 0; i + 1 < args.Length; i += 2)
                    if (args[i] is string key)
                        values[key] = args[i + 1];
                return values;
            }

            for (int i = 0; i < args.Length; i++)
                values[i.ToString()] = args[i];
            return values;
        }
    }
}
