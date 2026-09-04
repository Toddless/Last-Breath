namespace Core.Data.QuestData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Narrative;
    using Narrative.Quests;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Schema;

    /// <summary>Raw shape of a Quests catalog file. Conditions and actions stay as JTokens here —
    /// the QuestProvider runs them through the narrative parsers and drops broken quests whole.</summary>
    public record QuestsData
    {
        [JsonProperty("quests")] public List<QuestEntry> Quests { get; init; } = [];
    }

    /// <remarks>The json names are constants because a finding of the cross-checks is addressed the way
    /// the file is written: the reader that names a place and the attribute that reads it are one word.</remarks>
    public record QuestEntry
    {
        public const string GiverKey = "giverNpcId";

        public const string FactionKey = "faction";

        public const string TurnInKey = "turnInNpcIds";

        public const string DeclinePolicyKey = "declinePolicy";

        public const string AcceptConditionsKey = "acceptConditions";

        public const string StagesKey = "stages";

        public const string RewardsKey = "rewards";

        public const string OnAcceptKey = "onAccept";

        public const string OnDeclineKey = "onDecline";

        public const string OnFailKey = "onFail";

        /// <summary>What the decline policy is read as when the key is not written.</summary>
        public const string DefaultDeclinePolicy = nameof(Narrative.Quests.DeclinePolicy.CanReturn);

        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty(GiverKey)][CatalogRef(DataCatalog.Npc)] public string GiverNpcId { get; init; } = string.Empty;

        /// <summary>Faction the quest belongs to; a turn-in candidate must still carry it
        /// (a giver risen as undead is not the person you promised the blade to).</summary>
        [JsonProperty(FactionKey)][EnumOf(typeof(Fractions))] public string? Faction { get; init; }

        [JsonProperty("tier")] public int Tier { get; init; } = 1;
        [JsonProperty("repeatable")] public bool Repeatable { get; init; }
        [JsonProperty(TurnInKey)][CatalogRef(DataCatalog.Npc)] public List<string> TurnInNpcIds { get; init; } = [];
        [JsonProperty(DeclinePolicyKey)][EnumOf(typeof(DeclinePolicy))] public string DeclinePolicy { get; init; } = DefaultDeclinePolicy;
        [JsonProperty("declineCooldownHours")] public int DeclineCooldownHours { get; init; }

        /// <summary>False makes the quest unloseable — deadline, a lost turn-in NPC or a decline
        /// under a Fail policy cannot bury it. Omitted = true: quests fail as before.</summary>
        [JsonProperty("canFail")] public bool CanFail { get; init; } = true;

        /// <summary>Game hours to finish after accepting; 0 = no deadline.</summary>
        [JsonProperty("timeLimitHours")] public int TimeLimitHours { get; init; }

        [JsonProperty(AcceptConditionsKey)] public JToken? AcceptConditions { get; init; }
        [JsonProperty(StagesKey)] public List<QuestStageEntry> Stages { get; init; } = [];
        [JsonProperty(RewardsKey)] public QuestRewardsEntry Rewards { get; init; } = new();
        [JsonProperty(OnAcceptKey)] public JToken? OnAccept { get; init; }
        [JsonProperty(OnDeclineKey)] public JToken? OnDecline { get; init; }
        [JsonProperty(OnFailKey)] public JToken? OnFail { get; init; }
    }

    public record QuestStageEntry
    {
        public const string ObjectivesKey = "objectives";

        public const string TransitionsKey = "transitions";

        public const string OutcomeKey = "outcome";

        public const string OnEnterKey = "onEnter";

        public const string OnCompleteKey = "onComplete";

        /// <summary>Names the stage within its own quest; the routes and the save state are written
        /// with it. Points into no catalog.</summary>
        [JsonProperty("id")][NotARef] public string Id { get; init; } = string.Empty;

        [JsonProperty(ObjectivesKey)] public List<QuestObjectiveEntry> Objectives { get; init; } = [];

        /// <summary>Routes out of the stage, tried in order. Empty or absent = the next stage of the
        /// list, which is what a linear quest writes.</summary>
        [JsonProperty(TransitionsKey)] public List<QuestTransitionEntry> Transitions { get; init; } = [];

        /// <summary>Present = the stage ends the quest with this named outcome. Mutually exclusive
        /// with transitions: an ending leads nowhere.</summary>
        [JsonProperty(OutcomeKey)] public QuestOutcomeEntry? Outcome { get; init; }

        [JsonProperty(OnEnterKey)] public JToken? OnEnter { get; init; }
        [JsonProperty(OnCompleteKey)] public JToken? OnComplete { get; init; }
    }

    /// <summary>One route out of a stage. No conditions = unconditional, so such a route belongs
    /// last — the first transition whose conditions all hold wins.</summary>
    public record QuestTransitionEntry
    {
        public const string ToKey = "to";

        public const string ConditionsKey = "conditions";

        /// <summary>Id of a stage of THIS quest — where the route leads. Points into no catalog.</summary>
        [JsonProperty(ToKey)][NotARef] public string To { get; init; } = string.Empty;

        [JsonProperty(ConditionsKey)] public JToken? Conditions { get; init; }
    }

    /// <summary>A named ending of the quest. Its rewards are paid instead of the quest-wide ones;
    /// "fails": true buries the quest on the spot instead of offering a turn-in.</summary>
    public record QuestOutcomeEntry
    {
        public const string FailsKey = "fails";

        /// <summary>Names the ending within its own quest; the save keeps the outcome by it. Points
        /// into no catalog.</summary>
        [JsonProperty("id")][NotARef] public string Id { get; init; } = string.Empty;

        [JsonProperty(FailsKey)] public bool Fails { get; init; }
        [JsonProperty(QuestEntry.RewardsKey)] public QuestRewardsEntry? Rewards { get; init; }
    }

    /// <summary>Exactly one of condition/counter: a condition is a boolean predicate (retroactive
    /// by nature), a counter tracks a fact with visible X/N progress and an optional baseline.</summary>
    public record QuestObjectiveEntry
    {
        public const string ConditionKey = "condition";

        public const string CounterKey = "counter";

        /// <summary>Names the objective within its stage; its journal line is worded under it. Points
        /// into no catalog.</summary>
        [JsonProperty("id")][NotARef] public string Id { get; init; } = string.Empty;

        [JsonProperty(ConditionKey)] public JToken? Condition { get; init; }
        [JsonProperty(CounterKey)] public QuestCounterEntry? Counter { get; init; }
        [JsonProperty("optional")] public bool Optional { get; init; }
        [JsonProperty("hidden")] public bool Hidden { get; init; }
    }

    public record QuestCounterEntry
    {
        public const string KeyKey = "key";

        /// <summary>The fact whose count the objective watches — the same free-form key a dialogue writes
        /// and a Fact condition asks about. Points into no catalog.</summary>
        [JsonProperty(KeyKey)][NotARef][Suggests(SuggestionSources.FactKeys)] public string Key { get; init; } = string.Empty;
        [JsonProperty("amount")] public int Amount { get; init; } = 1;

        /// <summary>True: kills from before the quest count. False: the counter starts at the
        /// stage entry snapshot — "kill ten wolves" means ten NEW wolves.</summary>
        [JsonProperty("retroactive")] public bool Retroactive { get; init; } = true;
    }

    public record QuestRewardsEntry
    {
        public const string ItemsKey = "items";

        public const string ActionsKey = "actions";

        [JsonProperty("influenceExp")] public int InfluenceExp { get; init; }
        [JsonProperty(ItemsKey)] public List<QuestRewardItemEntry> Items { get; init; } = [];
        [JsonProperty(ActionsKey)] public JToken? Actions { get; init; }
    }

    public record QuestRewardItemEntry
    {
        /// <summary>What is handed over, out of everywhere an item is written — the same targets the
        /// GiveItem action reads its id from, said here as the markup an editor reads.</summary>
        [JsonProperty(ItemReference.Key)]
        [CatalogRef(ItemReference.EquipItems)]
        [CatalogRef(ItemReference.Items)]
        [CatalogRef(ItemReference.Recipes)]
        [CatalogRef(ItemReference.Ornaments)]
        [CatalogRef(ItemReference.Resources, Section = ItemReference.UpgradeResources)]
        [CatalogRef(ItemReference.Resources, Section = ItemReference.CraftingResources)]
        public string ItemId { get; init; } = string.Empty;

        [JsonProperty(ItemReference.AmountKey)] public int Amount { get; init; } = ItemReference.DefaultAmount;
    }
}
