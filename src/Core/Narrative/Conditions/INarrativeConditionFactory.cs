namespace Core.Narrative.Conditions
{
    using Newtonsoft.Json.Linq;

    /// <summary>Builds one condition type from its json entry. New type = new factory class +
    /// DI registration (registry instead of a switch).</summary>
    public interface INarrativeConditionFactory
    {
        /// <summary>Discriminator matched against the json "type" property.</summary>
        string Type { get; }

        /// <summary>Null when the entry is broken (missing field, bad enum) — report first.</summary>
        INarrativeCondition? Create(JObject json, INarrativeConditionParser parser);
    }
}
