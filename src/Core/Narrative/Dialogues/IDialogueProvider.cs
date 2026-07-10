namespace Core.Narrative.Dialogues
{
    public interface IDialogueProvider
    {
        /// <summary>Null when the NPC has no dialogue (or it was dropped as broken — reported).</summary>
        DialogueDefinition? Get(string npcId);
    }
}
