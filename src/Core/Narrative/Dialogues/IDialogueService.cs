namespace Core.Narrative.Dialogues
{
    using System;
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// The single running conversation (single-player: there is at most one). The world does NOT
    /// pause: a battle starting force-ends the dialogue, which is safe because every choice is
    /// atomic — actions run the moment an option is picked, nothing waits for the conversation end.
    /// </summary>
    public interface IDialogueService
    {
        bool IsActive { get; }

        /// <summary>Current node prepared for the UI: keys, not localized strings.</summary>
        DialogueNodeView? Current { get; }

        /// <summary>The node or its options changed — re-render.</summary>
        event Action? Changed;

        event Action? Ended;

        /// <summary>False when the NPC has no dialogue or no entry rule matches.</summary>
        bool Start(string npcId, string? npcInstanceId, Fractions faction);

        void Choose(string optionId);

        void End();
    }

    public record DialogueNodeView(string NpcId, IReadOnlyList<DialogueLine> Lines, IReadOnlyList<DialogueOptionView> Options);

    /// <summary>Only visible options make it here; disabled ones render greyed out.</summary>
    public record DialogueOptionView(string Id, string TextKey, bool Enabled);
}
