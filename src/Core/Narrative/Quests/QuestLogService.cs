namespace Core.Narrative.Quests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ai.World.Skirmish;
    using Ai.World.Time;
    using Entity;
    using Events;
    using Facts;
    using Influence;
    using Inventory;
    using MessageBus;
    using MessageBus.Messages;
    using Save;

    public class QuestLogService : IQuestLogService, Session.ISessionResettable
    {
        private const int MinutesPerHour = 60;
        private const int MinutesPerDay = 1440;
        private const string GhostHintNotificationId = "UI_Quest_Ghost_Hint";
        private const string DeclinedReason = "Declined";
        private const string AbandonedReason = "Abandoned";
        private const string TimeOutReason = "TimeOut";
        private const string TurnInPoolGoneReason = "LastTurnInNpcGone";
        private const string OutcomeReason = "Outcome:";

        private readonly Dictionary<string, QuestState> _states = [];
        private readonly IQuestProvider _quests;
        private readonly IWorldFactsService _facts;
        private readonly IInventory _inventory;
        private readonly Items.IItemMinter _items;
        private readonly Items.IUniqueItemQuery _uniqueItems;
        private readonly IInfluenceMastery _influence;
        private readonly IWorldClock _clock;
        private readonly INpcWorldRegistry _registry;
        private readonly IGameEventBus _events;
        private readonly IGameMessageBus _messages;
        private readonly ILoadScope _loadScope;
        private bool _reevaluating;
        private bool _dirty;

        public QuestLogService(IQuestProvider quests, IWorldFactsService facts, IInventory inventory,
            Items.IItemMinter items, Items.IUniqueItemQuery uniqueItems, IInfluenceMastery influence, IWorldClock clock,
            INpcWorldRegistry registry, IGameEventBus events, IGameMessageBus messages, ILoadScope loadScope)
        {
            _quests = quests;
            _facts = facts;
            _inventory = inventory;
            _items = items;
            _uniqueItems = uniqueItems;
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

            var state = NewAttempt(questId);
            state.AcceptedAtMinutes = NowMinutes();
            state.StageId = quest.Stages[0].Id;
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
            var state = NewAttempt(questId);
            _states[questId] = state;

            // A refused failure leaves the offer returnable instead of the half-written state.
            if (quest.DeclinePolicy == DeclinePolicy.Fail && Fail(questId, DeclinedReason)) return;

            state.Status = QuestStatus.Declined;
            state.NextOfferAtMinutes = NowMinutes() + CooldownMinutes(quest);
            Publish(state);
        }

        public void Abandon(string questId)
        {
            var quest = _quests.Get(questId);
            if (quest == null || GetState(questId) is not { Status: QuestStatus.Active or QuestStatus.ReadyToTurnIn } state) return;

            if (quest.DeclinePolicy == DeclinePolicy.Fail && Fail(questId, AbandonedReason)) return;

            state.Status = QuestStatus.Declined;
            state.NextOfferAtMinutes = NowMinutes() + CooldownMinutes(quest);
            Publish(state);
        }

        public bool Fail(string questId, string reason)
        {
            var quest = _quests.Get(questId);
            if (quest == null || GetState(questId) is not { } state || state.Status is QuestStatus.Completed or QuestStatus.Failed) return false;

            if (!quest.CanFail)
            {
                Tracker.TrackInfo($"Quest '{questId}' refused to fail ({reason}): the quest is declared unloseable");
                return false;
            }

            Tracker.TrackInfo($"Quest '{questId}' failed: {reason}");
            Execute(quest.OnFail, NarrativeContext.Empty);
            state.Status = QuestStatus.Failed;
            Publish(state);
            return true;
        }

        public bool CanTurnIn(string questId) =>
            GetState(questId) is { Status: QuestStatus.ReadyToTurnIn } state
            && _quests.Get(questId) is { } quest
            && _inventory.GetAvailableCapacity() >= OwedRewardItems(quest, state).Count;

        public bool TurnIn(string questId, NarrativeContext context)
        {
            if (!CanTurnIn(questId)) return false;
            var quest = _quests.Get(questId)!;
            var state = GetState(questId)!;

            // The gate must not answer yes twice: paying items wakes the inventory event, and a turn-in is an
            // authorable action, so the quest stops being turn-innable before a single reward is minted.
            state.Status = QuestStatus.Completed;
            var rewards = RewardsOf(quest, state);
            GrantRewardItems(quest, state);
            if (rewards.InfluenceExp > 0) _influence.AddExperience(rewards.InfluenceExp);
            Execute(rewards.Actions, context);
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

        /// <summary>Silent wipe: no per-quest status events — the fresh journal simply starts empty.</summary>
        public void ResetSession() => _states.Clear();

        public void RestoreState(IEnumerable<QuestState> states)
        {
            _states.Clear();
            foreach (var state in states)
            {
                // A quest removed from the game by a patch: log and drop the state.
                if (_quests.Get(state.QuestId) is not { } quest)
                {
                    Tracker.TrackInfo($"Quest '{state.QuestId}' is no longer in the catalog: its state is dropped, the ledger of unique rewards with it");
                    continue;
                }

                // A state naming no stage stands on the first one; a stage a patch renamed away is
                // reported and the quest is put back on the first stage as the safe place to stand.
                var stage = quest.Stages.Count > 0 ? quest.Stage(state.StageId) ?? quest.Stages[0] : null;
                if (stage != null && stage.Id != state.StageId)
                {
                    if (state.StageId.Length > 0)
                        Tracker.TrackError($"Quest '{quest.Id}' has no stage '{state.StageId}' any more: it restarts on '{stage.Id}'");
                    state.StageId = stage.Id;
                }

                _states[state.QuestId] = state;
            }
        }

        /// <summary>A fresh attempt of the same quest: everything about the attempt starts blank, the
        /// ledger of artefacts already handed over does not — it is the quest's, not the attempt's.</summary>
        private QuestState NewAttempt(string questId)
        {
            var state = new QuestState(questId);
            if (GetState(questId) is not { } previous) return state;

            foreach (string itemId in previous.GrantedUniqueRewards)
                state.GrantedUniqueRewards.Add(itemId);
            return state;
        }

        /// <summary>What this turn-in pays: a quest that ended on a named outcome pays that ending's
        /// rewards INSTEAD of its quest-wide ones — an outcome declaring none pays nothing.</summary>
        private static QuestRewards RewardsOf(QuestDefinition quest, QuestState state) =>
            quest.Outcome(state.OutcomeId)?.Rewards ?? quest.Rewards;

        /// <summary>Reward items this turn-in still owes — everything but the one-of-a-kind rewards the
        /// quest already paid. The gate and the payout read the same list, so the bag is asked to hold
        /// exactly what is about to be minted.</summary>
        private static List<QuestRewardItem> OwedRewardItems(QuestDefinition quest, QuestState state) =>
            RewardsOf(quest, state).Items.Where(reward => !state.GrantedUniqueRewards.Contains(reward.ItemId)).ToList();

        /// <summary>Mints what the turn-in owes and writes every one-of-a-kind item down as handed over.
        /// That note is the whole of the guarantee: a repeat turn-in reads it and pays around it.</summary>
        private void GrantRewardItems(QuestDefinition quest, QuestState state)
        {
            foreach (var reward in OwedRewardItems(quest, state))
            {
                // Minted, not copied: an equip reward is a fresh roll of its blueprint.
                if (!_inventory.TryAddItem(_items.MintItem(reward.ItemId), reward.Amount))
                {
                    // The ledger says "the player holds it", so a bag that refused writes nothing down.
                    Tracker.TrackInfo($"Quest '{quest.Id}' could not hand over '{reward.ItemId}': the bag refused it");
                    continue;
                }

                if (!_uniqueItems.IsUnique(reward.ItemId)) continue;

                state.GrantedUniqueRewards.Add(reward.ItemId);
                Tracker.TrackInfo($"Quest '{quest.Id}' handed out the unique reward '{reward.ItemId}': a repeat turn-in pays everything else, not this");
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

            // A quest that refused to fail keeps advancing past its deadline.
            if (DeadlinePassed(quest, state) && Fail(quest.Id, TimeOutReason)) return;

            // An ending is terminal even when the quest refused to be buried by it: a reached outcome
            // must not send the stage through its exit a second time.
            while (state.Status == QuestStatus.Active && state.OutcomeId == null
                && CurrentStage(quest, state) is { } stage && StageCompleted(quest, state))
                if (!LeaveStage(quest, state, stage)) return;
        }

        /// <summary>Walks a finished stage's route out; false = the quest goes no further this pass.
        /// A branch whose every route is still shut stays on its stage with its objectives met — the
        /// next change of the world asks again — so onComplete runs exactly once, on leaving.</summary>
        private bool LeaveStage(QuestDefinition quest, QuestState state, QuestStageDefinition stage)
        {
            string? next = NextStageId(quest, stage);
            if (next == null && stage.Transitions.Count > 0) return false;

            Execute(stage.OnComplete, NarrativeContext.Empty);

            if (stage.Outcome is { } outcome)
            {
                ReachOutcome(quest, state, outcome);
                return false;
            }

            if (next != null) return EnterStage(quest, state, next);

            state.Status = QuestStatus.ReadyToTurnIn;
            Publish(state);
            return false;
        }

        /// <summary>Where a finished stage leads: the first transition whose conditions all hold, or the
        /// next stage of the list when it declares none. Null = nowhere — an ending, the last stage, or
        /// a branch still waiting for one of its routes to open.</summary>
        private static string? NextStageId(QuestDefinition quest, QuestStageDefinition stage)
        {
            if (stage.Outcome != null) return null;
            if (stage.Transitions.Count > 0)
                return stage.Transitions
                    .FirstOrDefault(transition => transition.Conditions.All(condition => condition.IsMet(NarrativeContext.Empty)))
                    ?.ToStageId;

            int index = StageIndex(quest, stage.Id) + 1;
            return index > 0 && index < quest.Stages.Count ? quest.Stages[index].Id : null;
        }

        /// <summary>The quest ends on a named ending: a failing one buries it through the single failure
        /// gate (onFail included), the rest wait for the turn-in that pays the ending's own rewards.
        /// The catalog refuses a failing ending on a quest that cannot fail, so a refusal here means
        /// the definition came from somewhere the check does not guard.</summary>
        private void ReachOutcome(QuestDefinition quest, QuestState state, QuestOutcomeDefinition outcome)
        {
            state.OutcomeId = outcome.Id;
            if (outcome.Fails)
            {
                if (!Fail(quest.Id, OutcomeReason + outcome.Id))
                    Tracker.TrackError($"Quest '{quest.Id}': outcome '{outcome.Id}' declares a failing outcome, but the quest cannot fail: it stops on the ending unfinished");
                return;
            }

            state.Status = QuestStatus.ReadyToTurnIn;
            Publish(state);
        }

        private bool EnterStage(QuestDefinition quest, QuestState state, string stageId)
        {
            if (quest.Stage(stageId) is not { } stage)
            {
                Tracker.TrackError($"Quest '{quest.Id}' has no stage '{stageId}': the route out of '{state.StageId}' leads nowhere");
                return false;
            }

            state.StageId = stageId;
            SnapshotBaselines(state, stage);
            Execute(stage.OnEnter, NarrativeContext.Empty);
            _events.Publish(new QuestStageAdvancedEvent(quest.Id, stageId));
            return true;
        }

        private static int StageIndex(QuestDefinition quest, string stageId)
        {
            for (int index = 0; index < quest.Stages.Count; index++)
                if (quest.Stages[index].Id == stageId) return index;
            return -1;
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

        private static string BaselineKey(QuestState state, QuestCounter counter) => QuestState.BaselineKey(state.StageId, counter.FactKey);

        /// <summary>The A+B policy: one turn-in candidate left alive → a ghostly hint to hurry;
        /// the pool gone entirely (burned or risen with another face) → the quest fails unless it
        /// declares itself unloseable.</summary>
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
                    // The one call that drops the answer: the hint below needs a survivor, so an empty
                    // pool has nothing left to do whether the quest was buried or refused to fail.
                    Fail(quest.Id, TurnInPoolGoneReason);
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

        private static QuestStageDefinition? CurrentStage(QuestDefinition quest, QuestState state) => quest.Stage(state.StageId);

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
