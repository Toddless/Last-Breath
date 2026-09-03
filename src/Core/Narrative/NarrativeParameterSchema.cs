namespace Core.Narrative
{
    /// <summary>Builds the parameters a narrative factory declares. The game states them in its own
    /// terms; turning them into an editor's schema is that editor's adapter to do.</summary>
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

        public static NarrativeRecordSpec Of(string type, params NarrativeParameterSpec[] parameters) =>
            new() { TypeName = type, Parameters = parameters };

        /// <summary>Text; naming catalogs makes it a reference into them.</summary>
        public static NarrativeParameterSpec Text(string jsonName, bool required, params string[] catalogs) =>
            catalogs.Length == 0
                ? new NarrativeParameterSpec { JsonName = jsonName, Kind = NarrativeParameterKind.Text, Required = required }
                : new NarrativeParameterSpec
                {
                    JsonName = jsonName,
                    Kind = NarrativeParameterKind.Reference,
                    Required = required,
                    Catalogs = catalogs
                };

        /// <summary>Text the parser reads with a fallback, which is what makes writing it optional.</summary>
        public static NarrativeParameterSpec Text(string jsonName, string fallback) =>
            new() { JsonName = jsonName, Kind = NarrativeParameterKind.Text, Default = fallback };

        /// <summary>Text naming something no catalog holds — a spot in a scene, a key of the author's own
        /// invention. Refused as a reference rather than left silent, so a name ending in "Id" that points
        /// nowhere reads as a decision.</summary>
        public static NarrativeParameterSpec FreeText(string jsonName, bool required) =>
            new() { JsonName = jsonName, Kind = NarrativeParameterKind.Text, Required = required, RefusedAsReference = true };

        /// <summary>A number the parser reads with a fallback, which is what makes writing it optional.</summary>
        public static NarrativeParameterSpec Integer(string jsonName, int fallback) =>
            new() { JsonName = jsonName, Kind = NarrativeParameterKind.Integer, Default = fallback };

        /// <summary>A whole number with no fallback behind it: the parser refuses the entry without one.</summary>
        public static NarrativeParameterSpec Integer(string jsonName, bool required) =>
            new() { JsonName = jsonName, Kind = NarrativeParameterKind.Integer, Required = required };

        /// <summary>A list of references into the named catalogs. It carries no default because absence and
        /// an empty list are different answers — what a missing list means is the factory's to document.</summary>
        public static NarrativeParameterSpec References(string jsonName, params string[] catalogs) =>
            new() { JsonName = jsonName, Kind = NarrativeParameterKind.References, Catalogs = catalogs };

        /// <summary>Every member of the enum the parser reads the key into.</summary>
        public static NarrativeParameterSpec Enum<TEnum>(string jsonName, bool required = true)
            where TEnum : struct, System.Enum =>
            Choice(jsonName, required, System.Enum.GetNames<TEnum>());

        /// <summary>The members offered where they are not an enum's whole set — a word the parser treats
        /// specially, or a member the condition could never be met with.</summary>
        public static NarrativeParameterSpec Choice(string jsonName, bool required, params string[] members) =>
            new() { JsonName = jsonName, Kind = NarrativeParameterKind.Choice, Required = required, Choices = members };

        public static NarrativeParameterSpec Condition(string jsonName) =>
            new()
            {
                JsonName = jsonName,
                Kind = NarrativeParameterKind.NestedCondition,
                Required = true,
                Documentation = NestedConditionNote
            };

        public static NarrativeParameterSpec Conditions(string jsonName) =>
            new()
            {
                JsonName = jsonName,
                Kind = NarrativeParameterKind.NestedConditions,
                Required = true,
                Documentation = NestedConditionNote
            };
    }
}
