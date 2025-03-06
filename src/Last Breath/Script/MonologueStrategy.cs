namespace Playground.Script
{
    using System.Collections.Generic;
    using Playground.Localization;

    public class MonologueStrategy(Player player) : IDialogueStrategy
    {
        private Player _player = player;

        public DialogueNode? GetNextDialogueNode(string firstNode = "FirstMeeting") => _player.Dialogs.GetValueOrDefault(firstNode);
    }
}
