namespace LastBreath.Script
{
    using LastBreath.Localization;

    public interface IDialogueStrategy
    {
        DialogueNode? GetNextDialogueNode(string firstNode);

        void EndDialogue();
    }
}
