namespace Core.Narrative.Dialogues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Actions;
    using Conditions;
    using Data;
    using Data.DialogueData;
    using Data.GameData;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Loads the Dialogues catalog. Strict per dialogue: a broken condition/action/node reference
    /// drops the WHOLE dialogue with a report — the graph must not run with holes in it.
    /// </summary>
    public class DialogueProvider(INarrativeConditionParser conditions, INarrativeActionParser actions) : IDialogueProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, DialogueDefinition> _dialogues = [];

        public IReadOnlyCollection<DialogueDefinition> All => _dialogues.Values;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Dialogues];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<DialoguesData>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize dialogues file '{file.FileName}'");

            foreach (var entry in data.Dialogues)
            {
                var dialogue = ParseDialogue(entry);
                if (dialogue != null) _dialogues[dialogue.NpcId] = dialogue;
            }
        }

        public DialogueDefinition? Get(string npcId) => _dialogues.GetValueOrDefault(npcId);

        private DialogueDefinition? ParseDialogue(DialogueEntry entry)
        {
            if (entry.NpcId.Length == 0 || entry.Nodes.Count == 0 || entry.EntryRules.Count == 0)
            {
                Tracker.TrackError($"Dialogue '{entry.NpcId}' is broken: npcId, nodes and entryRules are required");
                return null;
            }

            try
            {
                var nodes = entry.Nodes.Select(ParseNode).ToDictionary(node => node.Id);

                var rules = entry.EntryRules
                    .Select(rule => new DialogueEntryRule(rule.Priority, ParseConditions(rule.Conditions), rule.Node))
                    .OrderByDescending(rule => rule.Priority)
                    .ToList();

                var dialogue = new DialogueDefinition(entry.NpcId, rules, nodes);
                ValidateReferences(dialogue);
                return dialogue;
            }
            catch (Exception exception)
            {
                Tracker.TrackError($"Dialogue '{entry.NpcId}' dropped: {exception.Message}");
                return null;
            }
        }

        private DialogueNode ParseNode(DialogueNodeEntry node)
        {
            if (node.Id.Length == 0 || node.Options.Count == 0)
                throw new InvalidOperationException($"node '{node.Id}' must have an id and at least one option (an exit line ends with next: null)");

            return new DialogueNode(
                node.Id,
                node.Lines.Select(line => new DialogueLine(EnumParser.ParseEnum<DialogueSpeaker>(line.Speaker), line.Key)).ToList(),
                ParseActions(node.OnEnter),
                node.Options.Select(ParseOption).ToList());
        }

        private DialogueOption ParseOption(DialogueOptionEntry option)
        {
            if (option.Id.Length == 0 || option.Key.Length == 0)
                throw new InvalidOperationException($"option '{option.Id}' must have an id and a text key");

            var speechCheck = option.SpeechCheck == null
                ? null
                : new DialogueSpeechCheck(option.SpeechCheck.Difficulty, option.SpeechCheck.FailNext, ParseActions(option.SpeechCheck.FailActions));

            return new DialogueOption(
                option.Id,
                option.Key,
                ParseConditions(option.VisibleConditions),
                ParseConditions(option.EnabledConditions),
                ParseActions(option.Actions),
                option.Next,
                option.OncePerConversation,
                option.OncePerGame,
                speechCheck);
        }

        private static void ValidateReferences(DialogueDefinition dialogue)
        {
            foreach (var rule in dialogue.EntryRules)
                RequireNode(dialogue, rule.NodeId, $"entry rule (priority {rule.Priority})");

            foreach (var node in dialogue.Nodes.Values)
            {
                foreach (var option in node.Options)
                {
                    if (option.NextNodeId != null) RequireNode(dialogue, option.NextNodeId, $"option '{node.Id}/{option.Id}'");
                    if (option.SpeechCheck?.FailNextNodeId is { } failNext) RequireNode(dialogue, failNext, $"speech check of '{node.Id}/{option.Id}'");
                }
            }
        }

        private static void RequireNode(DialogueDefinition dialogue, string nodeId, string referencedBy)
        {
            if (!dialogue.Nodes.ContainsKey(nodeId))
                throw new InvalidOperationException($"{referencedBy} points to missing node '{nodeId}'");
        }

        private List<INarrativeCondition> ParseConditions(JToken? array)
        {
            var parsed = conditions.ParseList(array);
            if (array != null && parsed.Count != array.Count())
                throw new InvalidOperationException("a condition entry is broken");
            return parsed;
        }

        private List<INarrativeAction> ParseActions(JToken? array)
        {
            var parsed = actions.ParseList(array);
            if (array != null && parsed.Count != array.Count())
                throw new InvalidOperationException("an action entry is broken");
            return parsed;
        }
    }
}
