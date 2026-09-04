namespace Tooling.Schema.Model
{
    /// <summary>One field of a record: the json name it is written under, what it holds, and whatever
    /// narrows that down — the catalogs a reference may point into, an enum's members, a range.</summary>
    public sealed record FieldSchema
    {
        /// <summary>The element of an array and the key or value of a dictionary are written without a
        /// name of their own.</summary>
        public const string Unnamed = "";

        public required string JsonName
        {
            get => field;
            init => field = SchemaGuard.NotNull(value, nameof(JsonName));
        }

        public required FieldKind Kind { get; init; }

        /// <summary>The key must be written even when its value is the default one.</summary>
        public bool Required { get; init; }

        /// <summary>What the game reads when the key is absent.</summary>
        public object? Default { get; init; }

        /// <summary>Member names offered for <see cref="FieldKind.Enum"/>; empty otherwise.</summary>
        public SchemaList<string> EnumValues { get; init; }

        /// <summary>Where a <see cref="FieldKind.Reference"/> points: a record is named if any one of the
        /// targets knows it. Loot positions name equipment, resources and recipes in one field; a target
        /// naming a section of its catalog is answered by that section alone.</summary>
        public SchemaList<ReferenceTarget> RefTargets { get; init; }

        /// <summary>An empty reference names nothing on purpose rather than being unfinished.</summary>
        public bool AllowEmpty { get; init; }

        /// <summary>The field was declared not to be a reference, whatever its name suggests: a decision
        /// told apart from a silence, which is what lets a check demand one or the other.</summary>
        public bool RefusedAsReference { get; init; }

        /// <summary>The list of words this text is usually answered with, by name; null where the author
        /// answers it out of his own head alone. An OPEN list, which is what tells it from an enum and a
        /// reference: a word the source does not know is written all the same, and only marked as one
        /// nobody has met. What the name means is the host's answer and never the schema's.</summary>
        public string? Suggests { get; init; }

        public NumericRange? Range
        {
            get => field;
            init => field = SchemaGuard.Ordered(value);
        }

        /// <summary>The key of a <see cref="FieldKind.Dictionary"/> when the author does not choose it
        /// freely — enum names, or references; null when any word will do.</summary>
        public FieldSchema? Key { get; init; }

        /// <summary>The element of an <see cref="FieldKind.Array"/> or the value of a
        /// <see cref="FieldKind.Dictionary"/>: both are one schema repeated.</summary>
        public FieldSchema? Item { get; init; }

        /// <summary>The nested record of an <see cref="FieldKind.Object"/>.</summary>
        public RecordSchema? Record { get; init; }

        /// <summary>Appended to the record's id to build the key, when the key is derived from the id
        /// rather than written out in full.</summary>
        public string? LocalizationSuffix { get; init; }

        /// <summary>What the field means, for the inspector — the DTO's own documentation.</summary>
        public string? Documentation { get; init; }

        /// <summary>Machinery: kept in the file, kept out of the inspector.</summary>
        public bool Hidden { get; init; }
    }
}
