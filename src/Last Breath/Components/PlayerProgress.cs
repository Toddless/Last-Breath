namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Script.Items;

    public class PlayerProgress
    {
        public HashSet<string> CompletedDialogues { get; } = [];
        public HashSet<string> InteractedNpcs { get; } = [];
        public HashSet<Item> SpecialItems { get; } = [];

        public void OnDialogueCompleted(string nodeId) => CompletedDialogues.Add(nodeId);
        public void OnNpcInteracted(string npcId) => InteractedNpcs.Add(npcId);
        public void OnItemCollected(Item item)
        {
        }
    }
}
