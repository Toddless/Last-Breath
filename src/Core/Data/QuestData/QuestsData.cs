namespace Core.Data.QuestData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>Raw shape of a Quests catalog file. Conditions and actions stay as JTokens here —
    /// the QuestProvider runs them through the narrative parsers and drops broken quests whole.</summary>
    public record QuestsData
    {
        [JsonProperty("quests")] public List<QuestEntry> Quests { get; init; } = [];
    }

    public record QuestEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("giverNpcId")] public string GiverNpcId { get; init; } = string.Empty;

        /// <summary>Faction the quest belongs to; a turn-in candidate must still carry it
        /// (a giver risen as undead is not the person you promised the blade to).</summary>
        [JsonProperty("faction")] public string? Faction { get; init; }

        [JsonProperty("tier")] public int Tier { get; init; } = 1;
        [JsonProperty("repeatable")] public bool Repeatable { get; init; }
        [JsonProperty("turnInNpcIds")] public List<string> TurnInNpcIds { get; init; } = [];
        [JsonProperty("declinePolicy")] public string DeclinePolicy { get; init; } = "CanReturn";
        [JsonProperty("declineCooldownHours")] public int DeclineCooldownHours { get; init; }

        /// <summary>Game hours to finish after accepting; 0 = no deadline.</summary>
        [JsonProperty("timeLimitHours")] public int TimeLimitHours { get; init; }

        [JsonProperty("acceptConditions")] public JToken? AcceptConditions { get; init; }
        [JsonProperty("stages")] public List<QuestStageEntry> Stages { get; init; } = [];
        [JsonProperty("rewards")] public QuestRewardsEntry Rewards { get; init; } = new();
        [JsonProperty("onAccept")] public JToken? OnAccept { get; init; }
        [JsonProperty("onDecline")] public JToken? OnDecline { get; init; }
        [JsonProperty("onFail")] public JToken? OnFail { get; init; }
    }

    public record QuestStageEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("objectives")] public List<QuestObjectiveEntry> Objectives { get; init; } = [];
        [JsonProperty("onEnter")] public JToken? OnEnter { get; init; }
        [JsonProperty("onComplete")] public JToken? OnComplete { get; init; }
    }

    /// <summary>Exactly one of condition/counter: a condition is a boolean predicate (retroactive
    /// by nature), a counter tracks a fact with visible X/N progress and an optional baseline.</summary>
    public record QuestObjectiveEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("condition")] public JToken? Condition { get; init; }
        [JsonProperty("counter")] public QuestCounterEntry? Counter { get; init; }
        [JsonProperty("optional")] public bool Optional { get; init; }
        [JsonProperty("hidden")] public bool Hidden { get; init; }
    }

    public record QuestCounterEntry
    {
        [JsonProperty("key")] public string Key { get; init; } = string.Empty;
        [JsonProperty("amount")] public int Amount { get; init; } = 1;

        /// <summary>True: kills from before the quest count. False: the counter starts at the
        /// stage entry snapshot — "kill ten wolves" means ten NEW wolves.</summary>
        [JsonProperty("retroactive")] public bool Retroactive { get; init; } = true;
    }

    public record QuestRewardsEntry
    {
        [JsonProperty("influenceExp")] public int InfluenceExp { get; init; }
        [JsonProperty("items")] public List<QuestRewardItemEntry> Items { get; init; } = [];
        [JsonProperty("actions")] public JToken? Actions { get; init; }
    }

    public record QuestRewardItemEntry
    {
        [JsonProperty("itemId")] public string ItemId { get; init; } = string.Empty;
        [JsonProperty("amount")] public int Amount { get; init; } = 1;
    }
}
