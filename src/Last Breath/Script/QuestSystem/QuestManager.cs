namespace Playground.Script.QuestSystem
{
    using System;
    using System.Linq;
    using Playground.Resource.Quests;
    using System.Collections.Generic;
    using Godot;

    public partial class QuestManager : Node
    {
        private Player? _player;
        private readonly HashSet<Quest> _activeQuests = [];
        private readonly HashSet<Quest> _failedQuests = [];
        private readonly HashSet<Quest> _completedQuests = [];
        private readonly HashSet<Quest> _cancelledQuests = [];

        public static QuestManager? Instance { get; private set; }

        public event Action<Quest>? QuestAccepted, QuestCompleted;

        public bool QuestCanBeAccepted(Quest quest)
        {
            if (_activeQuests.Contains(quest)) return false;
            if (_failedQuests.Contains(quest)) return false;
            if (_completedQuests.Contains(quest)) return false;
            if (_cancelledQuests.Contains(quest)) return false;
            if (CheckForConditions(quest))
            {
                return true;
            }
            return false;
        }

        public override void _EnterTree() => Instance ??= this;

        private bool CheckForConditions(Quest quest)
        {
            if (quest.Conditions.Count == 0) return true;
            if (quest.RequiredConditions > quest.Conditions.Count) return false;

            return quest.Conditions.Count(x => x.IsMet(_player!.Progress)) >= quest.RequiredConditions;
        }

        public void Initialize()
        {
            _player = GameManager.Instance.Player;
            SetEvents();
        }

        public void OnQuestCompleted(Quest quest)
        {
            _activeQuests.Remove(quest);
            _completedQuests.Add(quest);
            _player?.OnQuestCompleted(quest);
            QuestCompleted?.Invoke(quest);

        }

        public void OnQuestCancelled(Quest quest)
        {
            _activeQuests.Remove(quest);
            _cancelledQuests.Add(quest);
            quest.QuestStatus = QuestStatus.Canceled;
        }

        public void OnQuestAccepted(Quest quest)
        {
            _activeQuests.Add(quest);
            QuestAccepted?.Invoke(quest);
            quest.QuestStatus = QuestStatus.Progressing;
        }

        public void OnQuestFailed(Quest quest)
        {
            _activeQuests.Remove(quest);
            _failedQuests.Add(quest);
            quest.QuestStatus = QuestStatus.Failed;
        }

        private void SetEvents()
        {
            _player ??= GameManager.Instance.Player;
            _player.ItemCollected += OnItemCollected;
            _player.EnemyKilled += OnEnemyKilled;
            _player.LocationVisited += OnLocationVisited;
        }

        private void SetStatusCompleted(Quest quest) => quest.QuestStatus = QuestStatus.Completed;

        private void OnLocationVisited(string obj) => UpdateQuestObjectives(ObjectiveType.LocationVisit, obj);

        private void OnEnemyKilled(EnemyKilledEventArgs args) => UpdateQuestObjectives(ObjectiveType.EnemyKilling, args);

        private void OnItemCollected(string obj) => UpdateQuestObjectives(ObjectiveType.ItemCollection, obj);

        private void UpdateQuestObjectives(ObjectiveType type, object eventData)
        {
            foreach (var quest in _activeQuests)
            {
                if (quest.QuestObjective == null) continue;
                if (quest.QuestObjective.QuestObjectiveType == type)
                {
                    if (quest.QuestObjective.IsEventMatching(eventData))
                    {
                        quest.QuestObjective.CurrentAmount++;
                        CheckQuestCompletion(quest);
                    }
                }
            }
        }

        private void CheckQuestCompletion(Quest quest)
        {
            if (quest.QuestObjective!.IsCompleted)
            {
                SetStatusCompleted(quest);
            }
        }
    }
}
