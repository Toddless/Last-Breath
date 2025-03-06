namespace Playground.Script
{
    using Playground.Localization;

    public interface IDialogueStrategy
    {
        DialogueNode? GetNextDialogueNode(string firstNode = "FirstMeeting");
    }
}
