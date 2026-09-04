namespace Core.Data.DialogueData
{
    using System.Collections.Generic;
    using GameData;
    using Narrative.Dialogues;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Schema;

    /// <summary>Raw shape of a Dialogues catalog file. Conditions/actions stay as JTokens —
    /// the DialogueProvider parses them and drops a broken dialogue whole.</summary>
    public record DialoguesData
    {
        [JsonProperty("dialogues")] public List<DialogueEntry> Dialogues { get; init; } = [];
    }

    /// <summary>One dialogue per NPC DEFINITION id — every guard of that kind speaks it.</summary>
    /// <remarks>The json names are constants because a finding of the cross-checks is addressed the way
    /// the file is written: the reader that names a place and the attribute that reads it are one word.</remarks>
    public record DialogueEntry
    {
        public const string NpcIdKey = "npcId";

        public const string EntryRulesKey = "entryRules";

        public const string NodesKey = "nodes";

        [JsonProperty(NpcIdKey)][CatalogRef(DataCatalog.Npc)] public string NpcId { get; init; } = string.Empty;
        [JsonProperty(EntryRulesKey)] public List<DialogueEntryRuleEntry> EntryRules { get; init; } = [];
        [JsonProperty(NodesKey)] public List<DialogueNodeEntry> Nodes { get; init; } = [];
    }

    /// <summary>Highest priority whose conditions hold wins — that is how the greeting follows
    /// the world state (quest ready → "you have it?", Hatred → "get lost").</summary>
    public record DialogueEntryRuleEntry
    {
        public const string ConditionsKey = "conditions";

        public const string NodeKey = "node";

        [JsonProperty("priority")] public int Priority { get; init; }
        [JsonProperty(ConditionsKey)] public JToken? Conditions { get; init; }

        /// <summary>Id of a node of THIS dialogue: the conversation opens on it. Points into no
        /// catalog — the nodes are written beside it in the same record.</summary>
        [JsonProperty(NodeKey)][NotARef] public string Node { get; init; } = string.Empty;
    }

    public record DialogueNodeEntry
    {
        public const string OnEnterKey = "onEnter";

        public const string LinesKey = "lines";

        public const string OptionsKey = "options";

        /// <summary>Names the node within its own dialogue; the routes out of the other nodes are
        /// written with it. Points into no catalog.</summary>
        [JsonProperty("id")][NotARef] public string Id { get; init; } = string.Empty;

        [JsonProperty(OnEnterKey)] public JToken? OnEnter { get; init; }
        [JsonProperty(LinesKey)] public List<DialogueLineEntry> Lines { get; init; } = [];
        [JsonProperty(OptionsKey)] public List<DialogueOptionEntry> Options { get; init; } = [];
    }

    public record DialogueLineEntry
    {
        public const string SpeakerKey = "speaker";

        /// <summary>Json name of the localization key, shared with the option: text of a conversation is
        /// read under one word wherever it is written.</summary>
        public const string TextKey = "key";

        [JsonProperty(SpeakerKey)][EnumOf(typeof(DialogueSpeaker))] public string Speaker { get; init; } = "Npc";

        /// <summary>Localization key of the line, written out in full rather than derived from an id.</summary>
        [JsonProperty(TextKey)][LocalizedKey] public string Key { get; init; } = string.Empty;
    }

    public record DialogueOptionEntry
    {
        public const string VisibleConditionsKey = "visibleConditions";

        public const string EnabledConditionsKey = "enabledConditions";

        public const string ActionsKey = "actions";

        public const string NextKey = "next";

        public const string SpeechCheckKey = "speechCheck";

        /// <summary>Names the option within its node — the "once per game" bookkeeping is kept under
        /// it. Points into no catalog.</summary>
        [JsonProperty("id")][NotARef] public string Id { get; init; } = string.Empty;

        /// <summary>Localization key of the option, written out in full rather than derived from an id.</summary>
        [JsonProperty(DialogueLineEntry.TextKey)][LocalizedKey] public string Key { get; init; } = string.Empty;

        /// <summary>Fails → the option is hidden entirely.</summary>
        [JsonProperty(VisibleConditionsKey)] public JToken? VisibleConditions { get; init; }

        /// <summary>Fails → the option shows greyed out (the player SEES the locked door).</summary>
        [JsonProperty(EnabledConditionsKey)] public JToken? EnabledConditions { get; init; }

        [JsonProperty(ActionsKey)] public JToken? Actions { get; init; }

        /// <summary>Id of a node of THIS dialogue. Null/absent = choosing this ends the conversation.
        /// Points into no catalog.</summary>
        [JsonProperty(NextKey)][NotARef] public string? Next { get; init; }

        [JsonProperty("oncePerConversation")] public bool OncePerConversation { get; init; }
        [JsonProperty("oncePerGame")] public bool OncePerGame { get; init; }
        [JsonProperty(SpeechCheckKey)] public DialogueSpeechCheckEntry? SpeechCheck { get; init; }
    }

    /// <summary>An Influence roll with an INVISIBLE chance. Success runs actions/next as usual;
    /// failure runs failActions and routes to failNext (absent = conversation ends).</summary>
    public record DialogueSpeechCheckEntry
    {
        public const string FailNextKey = "failNext";

        public const string FailActionsKey = "failActions";

        [JsonProperty("difficulty")] public int Difficulty { get; init; }

        /// <summary>Id of a node of THIS dialogue; absent = the conversation ends. Points into no
        /// catalog.</summary>
        [JsonProperty(FailNextKey)][NotARef] public string? FailNext { get; init; }

        [JsonProperty(FailActionsKey)] public JToken? FailActions { get; init; }
    }
}
