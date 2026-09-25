namespace Core.PassiveTree.Rules
{
    using Newtonsoft.Json;

    /// <summary>PassiveTreeRules.json: what the tree costs, as opposed to what it contains. Property
    /// defaults mirror the shipped file so a pricing built before LoadAll (tests, sandboxes) charges the
    /// same numbers; the json is the balance surface and the only place they are tuned.</summary>
    public record PassiveTreeRulesData
    {
        [JsonProperty("respec")] public RespecPricingData Respec { get; init; } = new();
    }

    /// <summary>What undoing an allocation costs: gold per node, scaled by earned mastery (a late respec
    /// undoes a longer plan — meant as a decision, not a habit), and capped.</summary>
    public record RespecPricingData
    {
        [JsonProperty("goldPerNode")] public int GoldPerNode { get; init; } = 15;

        /// <summary>Added share of the price per earned mastery level.</summary>
        [JsonProperty("masteryScale")] public float MasteryScale { get; init; } = 0.04f;

        /// <summary>Ceiling on one respec, however many nodes go back at once.</summary>
        [JsonProperty("maxCost")] public int MaxCost { get; init; } = 2000;
    }
}
