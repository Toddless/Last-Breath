namespace Core.Narrative.Dialogues
{
    using System.Collections.Generic;

    public interface IDialogueProvider
    {
        /// <summary>Every dialogue the load KEPT. What a catalog writes and this does not hold was dropped
        /// as broken, which is the one way anything outside the loader can tell the two apart.</summary>
        IReadOnlyCollection<DialogueDefinition> All { get; }

        /// <summary>Null when the NPC has no dialogue (or it was dropped as broken — reported).</summary>
        DialogueDefinition? Get(string npcId);
    }
}
