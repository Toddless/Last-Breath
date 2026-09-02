namespace Core.Narrative.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public class NarrativeActionParser : INarrativeActionParser
    {
        private readonly Dictionary<string, INarrativeActionFactory> _factories;

        public NarrativeActionParser(IEnumerable<INarrativeActionFactory> factories) =>
            _factories = factories.ToDictionary(factory => factory.Type);

        public INarrativeAction? Parse(JToken token)
        {
            if (token is not JObject json)
            {
                Tracker.TrackError($"Narrative action must be a json object, got: {token.Type}");
                return null;
            }

            string type = json.Value<string>(NarrativeParameterSchema.TypeKey) ?? string.Empty;
            if (!_factories.TryGetValue(type, out var factory))
            {
                Tracker.TrackNotFound($"Narrative action type '{type}' has no registered factory");
                return null;
            }

            try
            {
                return factory.Create(json, this);
            }
            catch (Exception exception)
            {
                Tracker.TrackError($"Narrative action '{type}' failed to parse: {exception.Message}");
                return null;
            }
        }

        public List<INarrativeAction> ParseList(JToken? array)
        {
            var actions = new List<INarrativeAction>();
            if (array == null) return actions;

            foreach (var token in array)
            {
                var action = Parse(token);
                if (action != null) actions.Add(action);
            }

            return actions;
        }
    }
}
