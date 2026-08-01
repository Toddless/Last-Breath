namespace Core.Modifiers.Conditions
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// One catalog record into one live predicate. Types resolve through the registered factories, so a
    /// new predicate never touches this class; a type nobody registered is reported and refused, never
    /// silently downgraded to "always on". The catalog owns the ids — this class only reads the
    /// definition under one.
    /// </summary>
    public class ConditionParser
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

        /// <summary>The shipped factory set assembled without a container — tests and tooling.</summary>
        public static ConditionParser Default(ConditionTuning? tuning = null) => new(BuiltInFactories(tuning));

        /// <summary>Every predicate shipped with the game, ready for individual DI registration.</summary>
        public static IReadOnlyList<IConditionFactory> BuiltInFactories(ConditionTuning? tuning = null) =>
        [
            new ResourceThresholdConditionFactory(tuning ?? ConditionTuning.Default),
            new ResourceStateConditionFactory(),
            new StatusConditionFactory(),
            new EffectConditionFactory(),
            new StanceConditionFactory(),
            new TargetResourceThresholdConditionFactory(),
            new TargetStatusConditionFactory(),
        ];

        /// <summary>
        /// The predicate the record describes, or null when the record is broken or names a type nobody
        /// registered — reported first, so the entry costs itself and nothing else. The inversion flag is
        /// common to every type and is applied here rather than in the factories.
        /// </summary>
        public OwnerCondition? Parse(JObject record)
        {
            string type = record.Value<string>(ConditionFields.Type) ?? string.Empty;
            if (!_factories.TryGetValue(type, out var factory))
            {
                Tracker.TrackError($"Condition type '{type}' has no registered factory — skipping the entry");
                return null;
            }

            var built = Build(factory, record, type);
            if (built == null) return null;

            built.Negate = record.Value<bool?>(ConditionFields.Negate) ?? false;
            return built;
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
