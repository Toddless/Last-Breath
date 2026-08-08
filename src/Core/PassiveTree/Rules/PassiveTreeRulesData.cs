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

    /// <summary>
    /// What undoing an allocation costs. Gold per node, scaled by how far the character's own mastery
    /// has come — a late respec undoes a longer plan and is meant to be a decision rather than a habit —
    /// and capped, so the curve has an end that balance can move without the formula being rewritten.
    /// </summary>
    public record RespecPricingData
    {
        [JsonProperty("goldPerNode")] public int GoldPerNode { get; init; } = 15;

        /// <summary>Added share of the price per earned mastery level.</summary>
        [JsonProperty("masteryScale")] public float MasteryScale { get; init; } = 0.04f;

        /// <summary>Ceiling on one respec, however many nodes go back at once.</summary>
        [JsonProperty("maxCost")] public int MaxCost { get; init; } = 2000;
    }
}
