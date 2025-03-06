namespace Playground.Components
{
    using System.Collections.Generic;
    using Playground.Script.Items;

    public class PlayerProgress
    {
        public HashSet<string> CompletedDialogues { get; } = [];
        public HashSet<string> InteractedNpcs { get; } = [];
        public Dictionary<string, int> SpecialItems { get; } = [];
        public HashSet<string> CompletedQuests { get; } = [];
        public HashSet<string> Conditions { get; } = [];

        public void OnDialogueCompleted(string nodeId) => CompletedDialogues.Add(nodeId);
        public void OnNpcInteracted(string npcId) => InteractedNpcs.Add(npcId);
        public void OnQuestCompleted(string questId) => CompletedQuests.Add(questId);
        public void OnNewCondition(string name) => Conditions.Add(name);
        public void OnItemCollected(Item item)
        {
            if (!SpecialItems.TryAdd(item.SpecialId, item.Quantity))
            {
                SpecialItems[item.SpecialId] += item.Quantity;
            }
        }
    }
}
