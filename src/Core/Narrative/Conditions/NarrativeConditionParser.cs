namespace Core.Narrative.Conditions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public class NarrativeConditionParser : INarrativeConditionParser
    {
        private readonly Dictionary<string, INarrativeConditionFactory> _factories;

        public NarrativeConditionParser(IEnumerable<INarrativeConditionFactory> factories) =>
            _factories = factories.ToDictionary(factory => factory.Type);

        public INarrativeCondition? Parse(JToken token)
        {
            if (token is not JObject json)
            {
                Tracker.TrackError($"Narrative condition must be a json object, got: {token.Type}");
                return null;
            }

            string type = json.Value<string>(NarrativeParameterSchema.TypeKey) ?? string.Empty;
            if (!_factories.TryGetValue(type, out var factory))
            {
                Tracker.TrackNotFound($"Narrative condition type '{type}' has no registered factory");
                return null;
            }

            try
            {
                return factory.Create(json, this);
            }
            catch (Exception exception)
            {
                Tracker.TrackError($"Narrative condition '{type}' failed to parse: {exception.Message}");
                return null;
            }
        }

        public List<INarrativeCondition> ParseList(JToken? array)
        {
            var conditions = new List<INarrativeCondition>();
            if (array == null) return conditions;

            foreach (var token in array)
            {
                var condition = Parse(token);
                if (condition != null) conditions.Add(condition);
            }

            return conditions;
        }
    }
}
