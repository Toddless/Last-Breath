namespace Playground.Script
{
    using Playground.Localization;
    using Playground.Script.NPC;
    using Playground.Script.UI.View;

    public class OneToOneDialogueStrategy(BaseSpeakingNPC speakingNPC, Player player, DialogueWindow dialogueWindow) : IDialogueStrategy
    {
        private DialogueWindow _dialogueWindow = dialogueWindow;
        private readonly BaseSpeakingNPC _speakingNPC = speakingNPC;
        private readonly Player _player = player;

        public DialogueNode? GetNextDialogueNode(string nextNode)
        {
            DialogueNode? node = null;
            // all dialogues have unique ids
            if (_speakingNPC.Dialogs.TryGetValue(nextNode, out node)) return node;
            if (_player.Dialogs.TryGetValue(nextNode, out node)) return node;
            return null;
        }

        public void EndDialogue()
        {
            _dialogueWindow.Clear();
        }
    }
}
