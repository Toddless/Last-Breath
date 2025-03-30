namespace Playground.Script
{
    using System.Collections.Generic;
    using Playground.Localization;
    using Playground.Script.UI.View;

    public class MonologueStrategy(Player player, DialogueWindow window) : IDialogueStrategy
    {
        private readonly DialogueWindow _dialogueWindow = window;
        private readonly Player _player = player;

        public void EndDialogue()
        {
            _dialogueWindow.Clear();
            _dialogueWindow.CloseWindow();
        }

        public DialogueNode? GetNextDialogueNode(string firstNode) => _player.Dialogs.GetValueOrDefault(firstNode);
    }
}
