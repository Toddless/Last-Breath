namespace Core.Narrative.Dialogues
{
    using System.Collections.Generic;
    using Actions;
    using Conditions;

    public enum DialogueSpeaker : byte
    {
        Npc,
        Player,
    }

    /// <summary>Fully parsed dialogue of one NPC definition. Entry rules are pre-sorted by
    /// priority descending; all node references are validated at load.</summary>
    public record DialogueDefinition(
        string NpcId,
        IReadOnlyList<DialogueEntryRule> EntryRules,
        IReadOnlyDictionary<string, DialogueNode> Nodes);

    public record DialogueEntryRule(int Priority, IReadOnlyList<INarrativeCondition> Conditions, string NodeId);

    public record DialogueNode(
        string Id,
        IReadOnlyList<DialogueLine> Lines,
        IReadOnlyList<INarrativeAction> OnEnter,
        IReadOnlyList<DialogueOption> Options);

    public record DialogueLine(DialogueSpeaker Speaker, string TextKey);

    public record DialogueOption(
        string Id,
        string TextKey,
        IReadOnlyList<INarrativeCondition> VisibleConditions,
        IReadOnlyList<INarrativeCondition> EnabledConditions,
        IReadOnlyList<INarrativeAction> Actions,
        string? NextNodeId,
        bool OncePerConversation,
        bool OncePerGame,
        DialogueSpeechCheck? SpeechCheck);

    public record DialogueSpeechCheck(int Difficulty, string? FailNextNodeId, IReadOnlyList<INarrativeAction> FailActions);
}
