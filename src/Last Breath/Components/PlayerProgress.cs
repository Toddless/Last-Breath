namespace Playground.Components
{
    using Playground.Script.Enums;
    using Playground.Script.Items;
    using System.Collections.Generic;

    public class PlayerProgress
    {
        public HashSet<string> CompletedDialogues { get; } = [];
        public HashSet<string> InteractedNpcs { get; } = [];
        public Dictionary<string, int> QuestItems { get; } = [];
        public HashSet<string> CompletedQuests { get; } = [];

        public void OnDialogueCompleted(string nodeId) => CompletedDialogues.Add(nodeId);
        public void OnNpcInteracted(string npcId) => InteractedNpcs.Add(npcId);
        /// <summary>
        /// Tracking quests progress
        /// </summary>
        /// <param name="questId"></param>
        public void OnQuestCompleted(string questId) => CompletedQuests.Add(questId);
        /// <summary>
        /// Collecting quest items
        /// </summary>
        /// <param name="item"></param>
        public void OnQuestItemCollected(Item item)
        {
            if (item.Type == ItemType.Quest)
            {
                if (!QuestItems.TryAdd(item.Id, item.Quantity))
                {
                    // log
                }
            }
        }

        public void OnQuestItemRemoved(string id, int amount)
        {
            QuestItems[id] -= amount;
            if (QuestItems[id] < 1)
            {
                QuestItems.Remove(id);
                // raise event?
            }
        }
        /// <summary>
        /// Remove quest item after quest finished
        /// </summary>
        /// <param name="id"></param>
        public void OnQuestFinished(string id) => QuestItems.Remove(id);
    }
}
