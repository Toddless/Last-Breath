namespace Core.Modifiers.Conditions
{
    using System;
    using System.Collections.Generic;
    using Interfaces;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The single entry point from data to a live predicate. Types resolve through the registered
    /// factories, so a new predicate never touches this class; a type nobody registered is reported
    /// and refused, never silently downgraded to "always on".
    /// </summary>
    public class ConditionParser : IConditionParser
    {
        private readonly Dictionary<string, IConditionFactory> _factories;

        /// <summary>A type claimed twice is a wiring mistake — the built-in set registered next to a
        /// single factory, say — and it is reported and settled on the first claim: a host that starts
        /// with one predicate too many is still a host that starts.</summary>
        public ConditionParser(IEnumerable<IConditionFactory> factories)
        {
            _factories = new Dictionary<string, IConditionFactory>(StringComparer.OrdinalIgnoreCase);
            foreach (var factory in factories)
                if (!_factories.TryAdd(factory.Type, factory))
                    Tracker.TrackError($"Condition type '{factory.Type}' is registered more than once — keeping the factory registered first");
        }

        /// <summary>The parser every host gets until one registers its own factory set.</summary>
        public static ConditionParser Default(ConditionTuning? tuning = null) => new(BuiltInFactories(tuning));

        /// <summary>Every predicate shipped with the game, ready for individual DI registration.</summary>
        public static IReadOnlyList<IConditionFactory> BuiltInFactories(ConditionTuning? tuning = null) =>
        [
            new ResourceThresholdConditionFactory(tuning ?? ConditionTuning.Default),
            new ResourceStateConditionFactory(),
            new StatusConditionFactory(),
            new EffectConditionFactory(),
            new StanceConditionFactory(),
        ];

        public bool TryParse(JToken? token, out ICondition? condition)
        {
            condition = null;
            if (token == null || token.Type == JTokenType.Null) return true;

            if (token is not JObject json)
            {
                Tracker.TrackError($"Condition must be a json object, got: {token.Type}");
                return false;
            }

            string type = json.Value<string>(ConditionFields.Type) ?? string.Empty;
            if (!_factories.TryGetValue(type, out var factory))
            {
                Tracker.TrackError($"Condition type '{type}' has no registered factory — skipping the entry");
                return false;
            }

            var built = Build(factory, json, type);
            if (built == null) return false;

            built.Negate = json.Value<bool?>(ConditionFields.Negate) ?? false;
            condition = built;
            return true;
        }

        private static OwnerCondition? Build(IConditionFactory factory, JObject json, string type)
        {
            try
            {
                return factory.Create(json);
            }
            catch (Exception exception)
            {
                Tracker.TrackError($"Condition '{type}' failed to parse: {exception.Message}");
                return null;
            }
        }
    }
}
