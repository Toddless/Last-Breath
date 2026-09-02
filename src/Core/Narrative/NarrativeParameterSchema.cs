namespace Core.Narrative
{
    using Tooling.Schema.Model;

    /// <summary>Builds the parameter fields a narrative factory declares, in the same shape the tooling
    /// reflector builds them from a DTO, so one inspector reads schemas from both sources alike.</summary>
    public static class NarrativeParameterSchema
    {
        /// <summary>The key naming which factory reads an entry: the vocabulary's discriminator, written
        /// by the editor and read by the parser.</summary>
        public const string TypeKey = "type";

        /// <summary>The record name a nested condition carries in place of a record of its own. Its real
        /// shape is whichever factory its own <see cref="TypeKey"/> names, so the editor resolves it
        /// against the vocabulary instead of drawing fields — and an object that merely HAS no record
        /// (a cycle the reflector gave up on) is not mistaken for one.</summary>
        public const string NestedConditionRecord = "NarrativeCondition";

        private const string NestedConditionNote = "A narrative condition entry; its own \"type\" names the factory that reads it.";

        private static readonly RecordSchema s_nestedCondition = new() { TypeName = NestedConditionRecord, Fields = [] };

        public static RecordSchema Of(string type, params FieldSchema[] fields) =>
            new() { TypeName = type, Fields = fields };

        /// <summary>Text; naming catalogs makes it a reference into them.</summary>
        public static FieldSchema Text(string jsonName, bool required, params string[] catalogs) =>
            catalogs.Length == 0
                ? new FieldSchema { JsonName = jsonName, Kind = FieldKind.String, Required = required }
                : new FieldSchema { JsonName = jsonName, Kind = FieldKind.Reference, Required = required, RefCatalogs = catalogs };

        /// <summary>Text the parser reads with a fallback, which is what makes writing it optional.</summary>
        public static FieldSchema Text(string jsonName, string fallback) =>
            new() { JsonName = jsonName, Kind = FieldKind.String, Default = fallback };

        /// <summary>Text naming something no catalog holds — a spot in a scene, a key of the author's own
        /// invention. Refused as a reference rather than left silent, so a name ending in "Id" that points
        /// nowhere reads as a decision.</summary>
        public static FieldSchema FreeText(string jsonName, bool required) =>
            new() { JsonName = jsonName, Kind = FieldKind.String, Required = required, RefusedAsReference = true };

        /// <summary>A number the parser reads with a fallback, which is what makes writing it optional.</summary>
        public static FieldSchema Integer(string jsonName, int fallback) =>
            new() { JsonName = jsonName, Kind = FieldKind.Integer, Default = fallback };

        /// <summary>A whole number with no fallback behind it: the parser refuses the entry without one.</summary>
        public static FieldSchema Integer(string jsonName, bool required) =>
            new() { JsonName = jsonName, Kind = FieldKind.Integer, Required = required };

        /// <summary>A list of references into the named catalogs. It carries no default because absence and
        /// an empty list are different answers — what a missing list means is the factory's to document.</summary>
        public static FieldSchema References(string jsonName, params string[] catalogs) =>
            new()
            {
                JsonName = jsonName,
                Kind = FieldKind.Array,
                Item = new FieldSchema { JsonName = FieldSchema.Unnamed, Kind = FieldKind.Reference, RefCatalogs = catalogs }
            };

        /// <summary>Every member of the enum the parser reads the key into.</summary>
        public static FieldSchema Enum<TEnum>(string jsonName, bool required = true)
            where TEnum : struct, System.Enum =>
            Choice(jsonName, required, System.Enum.GetNames<TEnum>());

        /// <summary>The members offered where they are not an enum's whole set — a word the parser treats
        /// specially, or a member the condition could never be met with.</summary>
        public static FieldSchema Choice(string jsonName, bool required, params string[] members) =>
            new() { JsonName = jsonName, Kind = FieldKind.Enum, Required = required, EnumValues = members };

        public static FieldSchema Condition(string jsonName) =>
            new()
            {
                JsonName = jsonName,
                Kind = FieldKind.Object,
                Required = true,
                Record = s_nestedCondition,
                Documentation = NestedConditionNote
            };

        public static FieldSchema Conditions(string jsonName) =>
            new()
            {
                JsonName = jsonName,
                Kind = FieldKind.Array,
                Required = true,
                Item = new FieldSchema
                {
                    JsonName = FieldSchema.Unnamed,
                    Kind = FieldKind.Object,
                    Record = s_nestedCondition,
                    Documentation = NestedConditionNote
                }
            };
    }
}
