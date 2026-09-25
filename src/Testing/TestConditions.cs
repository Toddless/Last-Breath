namespace LastBreathTest
{
    using Core.Data.GameData;
    using Core.Modifiers.Conditions;

    /// <summary>The condition catalog as a consumer of it meets it: the real provider over a written file,
    /// so tests about lines held up by a condition resolve ids the way the game does — a hit is a fresh
    /// unattached predicate, a miss is a refusal.</summary>
    internal static class TestConditions
    {
        public const string Wounded = "Health_Wounded";
        public const string AtFullHealth = "Health_Full";

        private const string Catalog = """
            {
                "conditions": [
                    { "id": "Health_Wounded", "type": "ResourceState", "resource": "Health", "state": "Full", "negate": true },
                    { "id": "Health_Full", "type": "ResourceState", "resource": "Health", "state": "Full" }
                ]
            }
            """;

        /// <summary>A catalog holding <see cref="Wounded"/> and <see cref="AtFullHealth"/>.</summary>
        public static IConditionProvider Holding() => Read(Catalog);

        /// <summary>A catalog holding nothing — every id a line names is unknown to it.</summary>
        public static IConditionProvider Empty() => Read("""{ "conditions": [] }""");

        private static IConditionProvider Read(string json)
        {
            var provider = new ConditionProvider(ConditionParser.Default());
            provider.Apply(DataCatalog.Conditions, new GameDataFile("Conditions.json", json));
            return provider;
        }
    }
}
