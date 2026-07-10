namespace Core.Narrative.Conditions
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public interface INarrativeConditionParser
    {
        /// <summary>Null when the entry is broken — already reported to the Tracker; the caller
        /// decides how much to drop (convention: the whole record the condition gates).</summary>
        INarrativeCondition? Parse(JToken token);

        /// <summary>Broken entries are skipped with a report. Gating consumers should treat
        /// a count mismatch as a broken record — a lost clause must not soften a gate.</summary>
        List<INarrativeCondition> ParseList(JToken? array);
    }
}
