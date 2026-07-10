namespace Core.Narrative.Actions
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public interface INarrativeActionParser
    {
        /// <summary>Null when the entry is broken — already reported to the Tracker; the caller
        /// decides how much to drop (convention: the whole record the action belongs to).</summary>
        INarrativeAction? Parse(JToken token);

        /// <summary>Broken entries are skipped with a report. Consumers granting rewards should
        /// treat a count mismatch as a broken record — a lost action must not shortchange anyone.</summary>
        List<INarrativeAction> ParseList(JToken? array);
    }
}
