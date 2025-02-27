namespace Playground.Script.QuestSystem
{
    using System;
    using System.Linq;
    using Playground.Resource.Quests;
    using System.Collections.Generic;
    using Godot;
    using Playground.Script.Helpers;

    public class QuestManager
    {
        private readonly Dictionary<string, Quest> _allQuests = [];
        private readonly List<Quest> _quests = [];

        public event Action<Quest>? QuestAdded;

        public QuestManager()
        {
            LoadAllQuests();
        }

        public bool AddNewQuest(string questId)
        {
            if (!_allQuests.TryGetValue(questId, out var quest)) return false;
            if (_quests.Any(x => x.Id == quest.Id)) return false;
            if (!CheckForConditions(quest)) return false;

            var clonedQuest = quest.Duplicate(true) as Quest;
            _quests.Add(clonedQuest);
            QuestAdded?.Invoke(clonedQuest);
            return true;
        }

        private bool CheckForConditions(Quest quest)
        {
            if (quest.Conditions.Count == 0) return true;

            var player = GameManager.Instance.Player;
            var cnt = quest.Conditions.Count(x => x.IsMet(player.Progress));

            return quest.AllConditionsMustMet ? cnt == quest.Conditions.Count : cnt >= quest.RequiredConditions;
        }

        private void LoadAllQuests()
        {
            _allQuests.Clear();

            var quests = ResourceLoader.Load<QuestCollection>(ResourcePath.QuestData);

            foreach (var quest in quests.Quests)
            {

                try
                {
                    if (string.IsNullOrEmpty(quest.Id))
                    {
                        // log
                    }

                    if (_allQuests.ContainsKey(quest.Id))
                    {
                        // log
                        continue;
                    }
                    quest._Validate();
                    _allQuests[quest.Id] = quest;

                }
                catch (Exception ex)
                {
                    // Log ex.Message
                }
            }
        }
    }
}
