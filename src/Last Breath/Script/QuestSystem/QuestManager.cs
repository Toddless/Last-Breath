namespace Playground.Script.QuestSystem
{
    using System;
    using System.Linq;
    using Playground.Resource.Quests;
    using System.Collections.Generic;
    using Godot;

    public class QuestManager
    {
        private readonly List<Quest> _activeQuests = [];
        private readonly List<Quest> _failedQuests = [];
        private readonly List<Quest> _completedQuests = [];
        private readonly List<Quest> _cancelledQuests = [];

        public event Action<Quest>? QuestAccepted, QuestFailed, QuestCompleted, QuestCancelled;

        public bool QuestCanBeAccepted(Quest quest)
        {
            if (_activeQuests.Any(x => x.Id == quest.Id)) return false;
            if (_failedQuests.Any(x => x.Id == quest.Id)) return false;
            if (_completedQuests.Any(x => x.Id == quest.Id)) return false;
            if (_cancelledQuests.Any(x => x.Id == quest.Id)) return false;
            if (!CheckForConditions(quest)) return false;
            SetQuestEvents(quest);
            return true;
        }

        private bool CheckForConditions(Quest quest)
        {
            if (quest.Conditions.Count == 0) return true;

            var player = GameManager.Instance.Player;
            var cnt = quest.Conditions.Count(x => x.IsMet(player.Progress));

            return quest.AllConditionsMustMet ? cnt == quest.Conditions.Count : cnt >= quest.RequiredConditions;
        }

        private void RemoveQuestEvents(Quest quest)
        {
            quest.QuestAccepted -= OnQuestAccepted;
            quest.QuestCancelled -= OnQuestCancelled;
            quest.QuestFailed -= OnQuesFailed;
            quest.QuestCompleted -= OnQuestCompleted;
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
            RemoveQuestEvents(quest);
            QuestAccepted?.Invoke(quest);
        }
        private void OnQuesFailed(Quest quest)
        {
            _activeQuests.Remove(quest);
            _failedQuests.Add(quest);
            RemoveQuestEvents(quest);
            QuestFailed?.Invoke(quest);
        }
    }
}
