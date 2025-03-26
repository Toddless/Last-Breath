namespace Playground.Script
{
    using System.Collections.Generic;
    using Playground.Localization;

    public class MonologueStrategy(Player player) : IDialogueStrategy
    {
        private readonly Player _player = player;

        public DialogueNode? GetNextDialogueNode(string firstNode) => _player.Dialogs.GetValueOrDefault(firstNode);
    }
}
