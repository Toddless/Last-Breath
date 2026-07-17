namespace Core.Localization
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;

    /// <summary>The DI face of game text: translation + formatting. UI reaches it through the static Localization facade.</summary>
    public interface ILocalizationService
    {
        ILocalizationProvider Provider { get; }

        string Localize(string key);

        string LocalizeDescription(string id);

        /// <summary>Renders the &lt;id&gt;_Description template with named values ({Damage}, {Duration|turn|turns}).</summary>
        string RenderDescription(string id, IReadOnlyDictionary<string, object?> values, TextFormat format = TextFormat.Plain);

        /// <summary>Renders an arbitrary key as a template (battle log lines, notifications).</summary>
        string Render(string key, IReadOnlyDictionary<string, object?> values, TextFormat format = TextFormat.Plain);

        /// <summary>Formats a domain object through the registered ITextFormatter; unknown types render empty.</summary>
        string Format(object value, TextFormat format = TextFormat.Plain);

        string FormatModifier(IModifier modifier, float rangeMinValue, float rangeMaxValue, TextFormat format = TextFormat.Plain);

        /// <summary>The roll spread of a materialized item line ("40–60", "4–8%") for the Alt reveal;
        /// null when the line rolled no range (or the type carries no roll provenance).</summary>
        string? FormatRolledRange(object line, TextFormat format = TextFormat.Plain);

        /// <summary>One ParameterChange as a display value, unit-aware ("+5%", "-50", "+20%").</summary>
        string FormatParameterChange(EntityParameter parameter, float value, OperationType operation, TextFormat format = TextFormat.Plain);
    }
}
