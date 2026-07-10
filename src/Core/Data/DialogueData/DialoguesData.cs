namespace Core.Data.DialogueData
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>Raw shape of a Dialogues catalog file. Conditions/actions stay as JTokens —
    /// the DialogueProvider parses them and drops a broken dialogue whole.</summary>
    public record DialoguesData
    {
        [JsonProperty("dialogues")] public List<DialogueEntry> Dialogues { get; init; } = [];
    }

    /// <summary>One dialogue per NPC DEFINITION id — every guard of that kind speaks it.</summary>
    public record DialogueEntry
    {
        [JsonProperty("npcId")] public string NpcId { get; init; } = string.Empty;
        [JsonProperty("entryRules")] public List<DialogueEntryRuleEntry> EntryRules { get; init; } = [];
        [JsonProperty("nodes")] public List<DialogueNodeEntry> Nodes { get; init; } = [];
    }

    /// <summary>Highest priority whose conditions hold wins — that is how the greeting follows
    /// the world state (quest ready → "you have it?", Hatred → "get lost").</summary>
    public record DialogueEntryRuleEntry
    {
        [JsonProperty("priority")] public int Priority { get; init; }
        [JsonProperty("conditions")] public JToken? Conditions { get; init; }
        [JsonProperty("node")] public string Node { get; init; } = string.Empty;
    }

    public record DialogueNodeEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("onEnter")] public JToken? OnEnter { get; init; }
        [JsonProperty("lines")] public List<DialogueLineEntry> Lines { get; init; } = [];
        [JsonProperty("options")] public List<DialogueOptionEntry> Options { get; init; } = [];
    }

    public record DialogueLineEntry
    {
        [JsonProperty("speaker")] public string Speaker { get; init; } = "Npc";
        [JsonProperty("key")] public string Key { get; init; } = string.Empty;
    }

    public record DialogueOptionEntry
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;
        [JsonProperty("key")] public string Key { get; init; } = string.Empty;

        /// <summary>Fails → the option is hidden entirely.</summary>
        [JsonProperty("visibleConditions")] public JToken? VisibleConditions { get; init; }

        /// <summary>Fails → the option shows greyed out (the player SEES the locked door).</summary>
        [JsonProperty("enabledConditions")] public JToken? EnabledConditions { get; init; }

        [JsonProperty("actions")] public JToken? Actions { get; init; }

        /// <summary>Null/absent = choosing this ends the conversation.</summary>
        [JsonProperty("next")] public string? Next { get; init; }

        [JsonProperty("oncePerConversation")] public bool OncePerConversation { get; init; }
        [JsonProperty("oncePerGame")] public bool OncePerGame { get; init; }
        [JsonProperty("speechCheck")] public DialogueSpeechCheckEntry? SpeechCheck { get; init; }
    }

    /// <summary>An Influence roll with an INVISIBLE chance. Success runs actions/next as usual;
    /// failure runs failActions and routes to failNext (absent = conversation ends).</summary>
    public record DialogueSpeechCheckEntry
    {
        [JsonProperty("difficulty")] public int Difficulty { get; init; }
        [JsonProperty("failNext")] public string? FailNext { get; init; }
        [JsonProperty("failActions")] public JToken? FailActions { get; init; }
    }
}
