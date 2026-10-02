namespace LastBreathTest
{
    using Core.Data.GameData;
    using Core.Modifiers.Conditions;

    /// <summary>
    /// Condition catalogs assembled in memory, for the consumers that have to resolve an id before they
    /// can build anything. Built through the provider's own <see cref="IGameDataParticipant.Apply"/> so a
    /// test catalog goes through the same parse and the same refusals the shipped one does.
    /// </summary>
    internal static class ConditionCatalogs
    {
        /// <summary>Health under half — the predicate the tree tests gate a line with.</summary>
        public const string WhileWounded = "Health_Below_Half";

        /// <summary>An id no catalog here defines: what a line naming a condition nobody wrote points at.</summary>
        public const string Unwritten = "Health_Below_Nothing";

        /// <summary>The share of maximum health <see cref="WhileWounded"/> arms below.</summary>
        public const float WoundedShare = 0.5f;

        /// <summary>A catalog holding nothing: every id is refused, no id at all is still unconditional.</summary>
        public static ConditionProvider Empty() => Of();

        /// <summary>The catalog the tree tests use, with the one predicate they gate lines with.</summary>
        public static ConditionProvider Wounded() =>
            Of($$"""{ "id": "{{WhileWounded}}", "type": "{{ConditionTypes.ResourceThreshold}}", "resource": "Health", "value": {{WoundedShare}} }""");

        public static ConditionProvider Of(params string[] entries)
        {
            var provider = new ConditionProvider(ConditionParser.Default());
            provider.Apply(DataCatalog.Conditions, new GameDataFile("conditions.json", $$"""{ "{{ConditionFields.Entries}}": [ {{string.Join(", ", entries)}} ] }"""));

            return provider;
        }
    }
}
