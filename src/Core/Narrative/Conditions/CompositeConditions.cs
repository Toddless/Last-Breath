namespace Core.Narrative.Conditions
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>Composites are strict: one broken child breaks the whole composite (null from the
    /// factory). A silently dropped clause would soften a gate — fail closed instead.</summary>
    public class AllOfCondition(List<INarrativeCondition> conditions) : INarrativeCondition
    {
        public bool IsPreviewSafe => conditions.All(condition => condition.IsPreviewSafe);
        public bool IsMet(NarrativeContext context) => conditions.All(condition => condition.IsMet(context));
    }

    public class AnyOfCondition(List<INarrativeCondition> conditions) : INarrativeCondition
    {
        public bool IsPreviewSafe => conditions.All(condition => condition.IsPreviewSafe);
        public bool IsMet(NarrativeContext context) => conditions.Any(condition => condition.IsMet(context));
    }

    public class NotCondition(INarrativeCondition condition) : INarrativeCondition
    {
        public bool IsPreviewSafe => condition.IsPreviewSafe;
        public bool IsMet(NarrativeContext context) => !condition.IsMet(context);
    }

    public abstract class CompositeConditionFactory : INarrativeConditionFactory
    {
        protected const string ConditionsKey = "conditions";

        public abstract string Type { get; }

        public abstract NarrativeRecordSpec Parameters { get; }

        /// <summary>The shape is the same for every composite, the name is not: each one declares its own
        /// spec through this, so the vocabulary names the object rather than building one.</summary>
        protected static NarrativeRecordSpec SpecFor(string type) =>
            NarrativeParameterSchema.Of(type, NarrativeParameterSchema.Conditions(ConditionsKey));

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            var children = new List<INarrativeCondition>();
            foreach (var token in json.Value<JArray>(ConditionsKey) ?? [])
            {
                var child = parser.Parse(token);
                if (child == null) return null; // already reported
                children.Add(child);
            }

            if (children.Count != 0) return Build(children);

            Tracker.TrackError($"{Type} condition: {ConditionsKey} list is empty");
            return null;
        }

        protected abstract INarrativeCondition Build(List<INarrativeCondition> children);
    }

    public class AllOfConditionFactory : CompositeConditionFactory
    {
        private const string TypeName = "AllOf";

        public static readonly NarrativeRecordSpec Spec = SpecFor(TypeName);

        public override string Type => TypeName;

        public override NarrativeRecordSpec Parameters => Spec;

        protected override INarrativeCondition Build(List<INarrativeCondition> children) => new AllOfCondition(children);
    }

    public class AnyOfConditionFactory : CompositeConditionFactory
    {
        private const string TypeName = "AnyOf";

        public static readonly NarrativeRecordSpec Spec = SpecFor(TypeName);

        public override string Type => TypeName;

        public override NarrativeRecordSpec Parameters => Spec;

        protected override INarrativeCondition Build(List<INarrativeCondition> children) => new AnyOfCondition(children);
    }

    public class NotConditionFactory : INarrativeConditionFactory
    {
        private const string TypeName = "Not";
        private const string ConditionKey = "condition";

        public static readonly NarrativeRecordSpec Spec = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Condition(ConditionKey));

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => Spec;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            var inner = json[ConditionKey] is { } token ? parser.Parse(token) : null;
            if (inner != null) return new NotCondition(inner);

            Tracker.TrackError($"{TypeName} condition: inner {ConditionKey} is missing or broken");
            return null;
        }
    }
}
