namespace Playground.Script.QuestSystem
{
    using System;
    using System.Linq;
    using Playground.Resource.Quests;
    using System.Collections.Generic;
    using Godot;

    public class QuestManager
    {
        private readonly HashSet<Quest> _activeQuests = [];
        private readonly HashSet<Quest> _failedQuests = [];
        private readonly HashSet<Quest> _completedQuests = [];
        private readonly HashSet<Quest> _cancelledQuests = [];

        public event Action<Quest>? QuestAccepted, QuestFailed, QuestCompleted, QuestCancelled;

        public bool QuestCanBeAccepted(Quest quest)
        {
            if (_activeQuests.Contains(quest)) return false;
            if (_failedQuests.Contains(quest)) return false;
            if (_completedQuests.Contains(quest)) return false;
            if (_cancelledQuests.Contains(quest)) return false;
            if (!CheckForConditions(quest)) return false;
            return true;
        }

        private bool CheckForConditions(Quest quest)
        {
            if (quest.Conditions.Count == 0) return true;
            if(!quest.AllConditionsMustMet && quest.RequiredConditions > quest.Conditions.Count) return false;

            var player = GameManager.Instance.Player;
            var cnt = quest.Conditions.Count(x => x.IsMet(player.Progress));

            return quest.AllConditionsMustMet
                ? cnt == quest.Conditions.Count
                : cnt >= quest.RequiredConditions;
        }

        private void RemoveQuestEvents(Quest quest)
        {
            quest.QuestAccepted -= OnQuestAccepted;
            quest.QuestCancelled -= OnQuestCancelled;
            quest.QuestFailed -= OnQuestFailed;
            quest.QuestCompleted -= OnQuestCompleted;
        }

        private void SetQuestEvents(Quest quest)
        {
            quest.QuestAccepted += OnQuestAccepted;
            quest.QuestCancelled += OnQuestCancelled;
            quest.QuestFailed += OnQuestFailed;
            quest.QuestCompleted += OnQuestCompleted;
        }
        private void OnQuestCompleted(Quest quest)
        {
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);
            RemoveQuestEvents(quest);
            QuestCompleted?.Invoke(quest);
        }
        private void OnQuestCancelled(Quest quest)
        {
            _activeQuests.Remove(quest);
            _cancelledQuests.Add(quest);
            RemoveQuestEvents(quest);
            QuestCancelled?.Invoke(quest);
        }
        private void OnQuestAccepted(Quest quest)
        {
            _activeQuests.Add(quest);
            SetQuestEvents(quest);
            QuestAccepted?.Invoke(quest);
        }
        private void OnQuestFailed(Quest quest)
        {
            _activeQuests.Remove(quest);
            _failedQuests.Add(quest);
            RemoveQuestEvents(quest);
            QuestFailed?.Invoke(quest);
        }
    }
}
