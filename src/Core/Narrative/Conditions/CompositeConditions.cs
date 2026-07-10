namespace Core.Narrative.Conditions
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>Composites are strict: one broken child breaks the whole composite (null from the
    /// factory). A silently dropped clause would soften a gate — fail closed instead.</summary>
    public class AllOfCondition(List<INarrativeCondition> conditions) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => conditions.All(condition => condition.IsMet(context));
    }

    public class AnyOfCondition(List<INarrativeCondition> conditions) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => conditions.Any(condition => condition.IsMet(context));
    }

    public class NotCondition(INarrativeCondition condition) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => !condition.IsMet(context);
    }

    public abstract class CompositeConditionFactory : INarrativeConditionFactory
    {
        public abstract string Type { get; }

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            var children = new List<INarrativeCondition>();
            foreach (var token in json.Value<JArray>("conditions") ?? [])
            {
                var child = parser.Parse(token);
                if (child == null) return null; // already reported
                children.Add(child);
            }

            if (children.Count != 0) return Build(children);

            Tracker.TrackError($"{Type} condition: conditions list is empty");
            return null;
        }

        protected abstract INarrativeCondition Build(List<INarrativeCondition> children);
    }

    public class AllOfConditionFactory : CompositeConditionFactory
    {
        public override string Type => "AllOf";
        protected override INarrativeCondition Build(List<INarrativeCondition> children) => new AllOfCondition(children);
    }

    public class AnyOfConditionFactory : CompositeConditionFactory
    {
        public override string Type => "AnyOf";
        protected override INarrativeCondition Build(List<INarrativeCondition> children) => new AnyOfCondition(children);
    }

    public class NotConditionFactory : INarrativeConditionFactory
    {
        public string Type => "Not";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            var inner = json["condition"] is { } token ? parser.Parse(token) : null;
            if (inner != null) return new NotCondition(inner);

            Tracker.TrackError("Not condition: inner condition is missing or broken");
            return null;
        }
    }
}
