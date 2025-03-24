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

        public event Action<Quest>? QuestAccepted, QuestFailed, QuestCompleted, QuestCancelled;

        public static QuestManager Instance { get; private set; } = new();

        public bool QuestCanBeAccepted(Quest quest)
        {
            if (_activeQuests.Contains(quest)) return false;
            if (_failedQuests.Contains(quest)) return false;
            if (_completedQuests.Contains(quest)) return false;
            if (_cancelledQuests.Contains(quest)) return false;
            if (CheckForConditions(quest))
            {
                SetQuestEvents(quest);
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

        private void SetEvents()
        {
            _player ??= GameManager.Instance.Player;
            _player.ItemCollected += OnItemCollected;
            _player.EnemyKilled += OnEnemyKilled;
            _player.LocationVisited += OnLocationVisited;
        }

        private void OnLocationVisited(string obj) => UpdateQuestObjectives(ObjectiveType.LocationVisit, obj);

        private void OnEnemyKilled(string obj) => UpdateQuestObjectives(ObjectiveType.EnemyKilling, obj);

        private void OnItemCollected(string obj) => UpdateQuestObjectives(ObjectiveType.ItemCollection, obj);

        private void UpdateQuestObjectives(ObjectiveType type, string obj)
        {
            foreach (var quest in _activeQuests)
            {
                if (quest.QuestObjective == null) continue;
                if (quest.QuestObjective.QuestType == type && quest.QuestObjective.TargetId == obj)
                {
                    quest.QuestObjective.CurrentAmount++;
                    CheckQuestCompletion(quest);
                }
            }
        }

        private void CheckQuestCompletion(Quest quest)
        {
            if (quest.QuestObjective!.IsCompleted)
            {
                quest.CompletedQuest();
            }
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
            GD.Print($"Quest completed {quest.Id}");
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
