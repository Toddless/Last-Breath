namespace Core.Narrative.Quests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World.Skirmish;
    using Ai.World.Time;
    using Data;
    using Entity;
    using Events;
    using Events.GameEvents;
    using Facts;
    using Influence;
    using Inventory;
    using MessageBus;
    using Save;

    public class QuestLogService : IQuestLogService
    {
        private const int MinutesPerHour = 60;
        private const int MinutesPerDay = 1440;
        private const string GhostHintNotificationId = "UI_Quest_Ghost_Hint";

        private readonly Dictionary<string, QuestState> _states = [];
        private readonly IQuestProvider _quests;
        private readonly IWorldFactsService _facts;
        private readonly IInventory _inventory;
        private readonly IItemDataProvider _items;
        private readonly IInfluenceMastery _influence;
        private readonly IWorldClock _clock;
        private readonly INpcWorldRegistry _registry;
        private readonly IGameEventBus _events;
        private readonly IGameMessageBus _messages;
        private readonly ILoadScope _loadScope;
        private bool _reevaluating;
        private bool _dirty;

        public QuestLogService(IQuestProvider quests, IWorldFactsService facts, IInventory inventory,
            IItemDataProvider items, IInfluenceMastery influence, IWorldClock clock,
            INpcWorldRegistry registry, IGameEventBus events, IGameMessageBus messages, ILoadScope loadScope)
        {
            _quests = quests;
            _facts = facts;
            _inventory = inventory;
            _items = items;
            _influence = influence;
            _clock = clock;
            _registry = registry;
            _events = events;
            _messages = messages;
            _loadScope = loadScope;

            _facts.FactChanged += (_, _) => Reevaluate();
            _inventory.ItemAmountChanges += (_, _) => Reevaluate();
            _clock.HourPassed += _ => Reevaluate();
            _events.Subscribe<EntityDiedEvent>(_ => ReevaluateTurnInCandidates());
            _events.Subscribe<NpcFinalDeathEvent>(_ => ReevaluateTurnInCandidates());
            _events.Subscribe<NpcFactionChangedEvent>(_ => ReevaluateTurnInCandidates());
        }

        public IReadOnlyCollection<QuestState> States => _states.Values;

        public QuestState? GetState(string questId) => _states.GetValueOrDefault(questId);

        public QuestStatus? GetStatus(string questId) => GetState(questId)?.Status;

        public bool CanAccept(string questId, NarrativeContext context)
        {
            var quest = _quests.Get(questId);
            if (quest == null) return false;

            bool offerable = GetState(questId) switch
            {
                null => true,
                { Status: QuestStatus.Declined } state => NowMinutes() >= state.NextOfferAtMinutes,
                { Status: QuestStatus.Completed } => quest.Repeatable,
                _ => false,
            };

            return offerable && quest.AcceptConditions.All(condition => condition.IsMet(context));
        }

        public bool Accept(string questId, NarrativeContext context)
        {
            if (!CanAccept(questId, context)) return false;
            var quest = _quests.Get(questId)!;

            var state = new QuestState(questId) { AcceptedAtMinutes = NowMinutes() };
            _states[questId] = state;
            SnapshotBaselines(state, quest.Stages[0]);

            Execute(quest.OnAccept, context);
            Execute(quest.Stages[0].OnEnter, context);
            Publish(state);
            Reevaluate(); // retroactivity: stages already satisfied by the world close right now
            return true;
        }

        public void Decline(string questId, NarrativeContext context)
        {
            var quest = _quests.Get(questId);
            if (quest == null || GetState(questId) is { Status: QuestStatus.Active or QuestStatus.ReadyToTurnIn }) return;

            Execute(quest.OnDecline, context);
            var state = new QuestState(questId);
            _states[questId] = state;

            if (quest.DeclinePolicy == DeclinePolicy.Fail)
            {
                Fail(questId, "Declined");
                return;
            }

            state.Status = QuestStatus.Declined;
            state.NextOfferAtMinutes = NowMinutes() + CooldownMinutes(quest);
            Publish(state);
        }

        public void Abandon(string questId)
        {
            var quest = _quests.Get(questId);
            if (quest == null || GetState(questId) is not { Status: QuestStatus.Active or QuestStatus.ReadyToTurnIn } state) return;

            if (quest.DeclinePolicy == DeclinePolicy.Fail)
            {
                Fail(questId, "Abandoned");
                return;
            }

            state.Status = QuestStatus.Declined;
            state.NextOfferAtMinutes = NowMinutes() + CooldownMinutes(quest);
            Publish(state);
        }

        public void Fail(string questId, string reason)
        {
            var quest = _quests.Get(questId);
            if (quest == null || GetState(questId) is not { } state || state.Status is QuestStatus.Completed or QuestStatus.Failed) return;

            Tracker.TrackInfo($"Quest '{questId}' failed: {reason}");
            Execute(quest.OnFail, NarrativeContext.Empty);
            state.Status = QuestStatus.Failed;
            Publish(state);
        }

        public bool CanTurnIn(string questId) =>
            GetState(questId) is { Status: QuestStatus.ReadyToTurnIn }
            && _quests.Get(questId) is { } quest
            && _inventory.GetAvailableCapacity() >= quest.Rewards.Items.Count;

        public bool TurnIn(string questId, NarrativeContext context)
        {
            if (!CanTurnIn(questId)) return false;
            var quest = _quests.Get(questId)!;
            var state = GetState(questId)!;

            foreach (var reward in quest.Rewards.Items)
                _inventory.TryAddItem(_items.CopyItem(reward.ItemId), reward.Amount);
            if (quest.Rewards.InfluenceExp > 0) _influence.AddExperience(quest.Rewards.InfluenceExp);
            Execute(quest.Rewards.Actions, context);

            state.Status = QuestStatus.Completed;
            Publish(state);
            return true;
        }

        public (int Current, int Required) GetObjectiveProgress(string questId, string objectiveId)
        {
            if (GetState(questId) is not { } state || _quests.Get(questId) is not { } quest) return (0, 1);
            var objective = CurrentStage(quest, state)?.Objectives.FirstOrDefault(entry => entry.Id == objectiveId);
            if (objective == null) return (0, 1);

            if (objective.Counter is { } counter)
                return (Math.Clamp(CounterValue(state, counter), 0, counter.Amount), counter.Amount);
            return (objective.Condition!.IsMet(NarrativeContext.Empty) ? 1 : 0, 1);
        }

        public void RestoreState(IEnumerable<QuestState> states)
        {
            _states.Clear();
            foreach (var state in states)
            {
                // A quest removed from the game by a patch: log and drop the state.
                if (_quests.Get(state.QuestId) == null) continue;
                _states[state.QuestId] = state;
            }
        }

        private void Reevaluate()
        {
            if (_loadScope.IsLoading) return;
            if (_reevaluating)
            {
                _dirty = true; // an action just changed the world — rerun after this pass
                return;
            }

            _reevaluating = true;
            try
            {
                do
                {
                    _dirty = false;
                    foreach (var state in _states.Values.Where(entry => entry.Status is QuestStatus.Active).ToList())
                        EvaluateQuest(state);
                }
                while (_dirty);
            }
            finally
            {
                _reevaluating = false;
            }
        }

        private void EvaluateQuest(QuestState state)
        {
            var quest = _quests.Get(state.QuestId);
            if (quest == null) return;

            if (DeadlinePassed(quest, state))
            {
                Fail(quest.Id, "TimeOut");
                return;
            }

            while (state.Status == QuestStatus.Active && StageCompleted(quest, state))
            {
                var stage = quest.Stages[state.StageIndex];
                Execute(stage.OnComplete, NarrativeContext.Empty);

                if (state.StageIndex + 1 >= quest.Stages.Count)
                {
                    state.Status = QuestStatus.ReadyToTurnIn;
                    Publish(state);
                    return;
                }

                state.StageIndex++;
                var next = quest.Stages[state.StageIndex];
                SnapshotBaselines(state, next);
                Execute(next.OnEnter, NarrativeContext.Empty);
                _events.Publish(new QuestStageAdvancedEvent(quest.Id, state.StageIndex));
            }
        }

        private bool StageCompleted(QuestDefinition quest, QuestState state) =>
            CurrentStage(quest, state) is { } stage
            && stage.Objectives.All(objective => objective.IsOptional || ObjectiveMet(state, objective));

        private bool ObjectiveMet(QuestState state, QuestObjectiveDefinition objective) =>
            objective.Counter is { } counter
                ? CounterValue(state, counter) >= counter.Amount
                : objective.Condition!.IsMet(NarrativeContext.Empty);

        private int CounterValue(QuestState state, QuestCounter counter) =>
            _facts.GetCount(counter.FactKey) - state.CounterBaselines.GetValueOrDefault(BaselineKey(state, counter));

        private void SnapshotBaselines(QuestState state, QuestStageDefinition stage)
        {
            foreach (var objective in stage.Objectives)
                if (objective.Counter is { Retroactive: false } counter)
                    state.CounterBaselines[BaselineKey(state, counter)] = _facts.GetCount(counter.FactKey);
        }

        private static string BaselineKey(QuestState state, QuestCounter counter) => $"{state.StageIndex}:{counter.FactKey}";

        /// <summary>The A+B policy: one turn-in candidate left alive → a ghostly hint to hurry;
        /// the pool gone entirely (burned or risen with another face) → the quest fails.</summary>
        private void ReevaluateTurnInCandidates()
        {
            if (_loadScope.IsLoading) return;

            foreach (var state in _states.Values.Where(entry => entry.Status is QuestStatus.Active or QuestStatus.ReadyToTurnIn).ToList())
            {
                var quest = _quests.Get(state.QuestId);
                if (quest == null || quest.TurnInNpcIds.Count == 0) continue;

                var pool = _registry.All
                    .Where(participant => participant is IEntity entity
                        && quest.TurnInNpcIds.Contains(entity.Id)
                        && (quest.Faction == null || participant.Fraction == quest.Faction))
                    .ToList();

                if (pool.Count == 0)
                {
                    Fail(quest.Id, "LastTurnInNpcGone");
                    continue;
                }

                if (state.GhostHintShown || pool.Count(participant => participant.IsAlive) != 1) continue;
                state.GhostHintShown = true;
                _ = _messages.PublishMessageAsync(new SendNotificationMessageMessage(GhostHintNotificationId, NotificationCategory.System,
                    new Dictionary<string, object?> { ["Quest"] = Localization.Localization.Localize(quest.Id) }));
            }
        }

        private bool DeadlinePassed(QuestDefinition quest, QuestState state) =>
            quest.TimeLimitHours > 0 && NowMinutes() >= state.AcceptedAtMinutes + quest.TimeLimitHours * MinutesPerHour;

        private static QuestStageDefinition? CurrentStage(QuestDefinition quest, QuestState state) =>
            state.StageIndex < quest.Stages.Count ? quest.Stages[state.StageIndex] : null;

        private static int CooldownMinutes(QuestDefinition quest) =>
            quest.DeclinePolicy == DeclinePolicy.Cooldown ? quest.DeclineCooldownHours * MinutesPerHour : 0;

        private int NowMinutes() => _clock.Day * MinutesPerDay + _clock.MinuteOfDay;

        private void Execute(IReadOnlyList<Actions.INarrativeAction> actions, NarrativeContext context)
        {
            foreach (var action in actions)
                action.Execute(context);
        }

        private void Publish(QuestState state) => _events.Publish(new QuestStatusChangedEvent(state.QuestId, state.Status));
    }
}
