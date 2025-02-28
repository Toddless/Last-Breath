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
        private readonly List<Quest> _activeQuests = [];
        private readonly List<Quest> _failedQuests = [];
        private readonly List<Quest> _completedQuests = [];

        public event Action<Quest>? QuestAccepted, QuestFailed, QuestCompleted, QuestCancelled;

        public QuestManager()
        {
            LoadAllQuests();
        }

        public bool QuestCanBeAdded(string questId)
        {
            if (!_allQuests.TryGetValue(questId, out var quest)) return false;
            if (_activeQuests.Any(x => x.Id == quest.Id)) return false;
            return CheckForConditions(quest);
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
                    SetQuestEvents(quest);
                    _allQuests[quest.Id] = quest;

                }
                catch (Exception ex)
                {
                    // Log ex.Message
                }
            }
        }

        private void SetQuestEvents(Quest quest)
        {
            quest.QuestAccepted += OnQuestAccepted;
            quest.QuestCancelled += OnQuestCancelled;
            quest.QuestFailed += OnQuesFailed;
            quest.QuestCompleted += OnQuestCompleted;
        }
        private void OnQuestCompleted(Quest quest)
        {
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);
            QuestCompleted?.Invoke(quest);
        }
        private void OnQuestCancelled(Quest quest)
        {
            _activeQuests.Remove(quest);
            _allQuests.Add(quest.Id, quest);
            QuestCancelled?.Invoke(quest);
        }
        private void OnQuestAccepted(Quest quest)
        {
            _allQuests.Remove(quest.Id);
            _activeQuests.Add(quest);
            QuestAccepted?.Invoke(quest);
        }
        private void OnQuesFailed(Quest quest)
        {
            _activeQuests.Remove(quest);
            _failedQuests.Add(quest);
            QuestFailed?.Invoke(quest);
        }
    }
}
