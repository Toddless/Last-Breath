namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using Tooling.Schema.Model;

    /// <summary>Reads the narrative vocabulary as schemas: the game states what each condition and action
    /// is written with in its own terms, and this is the one place turning that into what an editor
    /// draws from.</summary>
    public static class NarrativeSchemas
    {
        private static readonly RecordSchema s_nestedCondition =
            new() { TypeName = NarrativeParameterSchema.NestedConditionRecord, Fields = [] };

        /// <summary>Every condition type the vocabulary was registered with.</summary>
        public static IReadOnlyList<RecordSchema> Conditions(IEnumerable<INarrativeConditionFactory> factories) =>
            [.. factories.Select(factory => ToSchema(factory.Parameters))];

        /// <summary>Every action type the vocabulary was registered with.</summary>
        public static IReadOnlyList<RecordSchema> Actions(IEnumerable<INarrativeActionFactory> factories) =>
            [.. factories.Select(factory => ToSchema(factory.Parameters))];

        public static RecordSchema ToSchema(NarrativeRecordSpec record) =>
            new() { TypeName = record.TypeName, Fields = [.. record.Parameters.Select(ToSchema)] };

        /// <summary>The one reading of a parameter kind: a kind with no arm here does not compile away
        /// quietly, it throws on the first entry carrying it.</summary>
        public static FieldSchema ToSchema(NarrativeParameterSpec parameter) => parameter.Kind switch
        {
            NarrativeParameterKind.Text => Field(parameter, FieldKind.String),
            NarrativeParameterKind.Integer => Field(parameter, FieldKind.Integer),
            NarrativeParameterKind.Number => Field(parameter, FieldKind.Number),
            NarrativeParameterKind.Boolean => Field(parameter, FieldKind.Boolean),
            NarrativeParameterKind.Choice => Field(parameter, FieldKind.Enum) with { EnumValues = [.. parameter.Choices] },
            NarrativeParameterKind.Reference => Field(parameter, FieldKind.Reference) with { RefTargets = Targets(parameter) },
            NarrativeParameterKind.References => Field(parameter, FieldKind.Array) with { Item = ReferenceItem(parameter) },
            NarrativeParameterKind.NestedCondition => Field(parameter, FieldKind.Object) with { Record = s_nestedCondition },
            NarrativeParameterKind.NestedConditions => Field(parameter, FieldKind.Array) with { Item = ConditionItem(parameter) },
            _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter.Kind, "The narrative parameter kind has no schema of its own."),
        };

        /// <summary>What every kind carries alike; what narrows it down is added by its own arm.</summary>
        private static FieldSchema Field(NarrativeParameterSpec parameter, FieldKind kind) =>
            new()
            {
                JsonName = parameter.JsonName,
                Kind = kind,
                Required = parameter.Required,
                Default = parameter.Default,
                RefusedAsReference = parameter.RefusedAsReference,
                Documentation = parameter.Documentation
            };

        private static FieldSchema ReferenceItem(NarrativeParameterSpec parameter) =>
            new() { JsonName = FieldSchema.Unnamed, Kind = FieldKind.Reference, RefTargets = Targets(parameter) };

        /// <summary>Where a narrative reference points, said in the tool's own words: the vocabulary states
        /// a catalog and, where it narrowed one, the section of it the game resolves ids against.</summary>
        private static SchemaList<ReferenceTarget> Targets(NarrativeParameterSpec parameter) =>
            [.. parameter.Targets.Select(target =>
                target.Section is null ? ReferenceTarget.Whole(target.Catalog) : new ReferenceTarget(target.Catalog, target.Section))];

        private static FieldSchema ConditionItem(NarrativeParameterSpec parameter) =>
            new()
            {
                JsonName = FieldSchema.Unnamed,
                Kind = FieldKind.Object,
                Record = s_nestedCondition,
                Documentation = parameter.Documentation
            };
    }
}
