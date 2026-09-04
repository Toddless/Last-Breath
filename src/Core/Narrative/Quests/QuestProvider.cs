namespace Core.Narrative.Quests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Actions;
    using Conditions;
    using Data;
    using Data.GameData;
    using Data.QuestData;
    using Enums;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads the Quests catalog and turns raw entries into fully parsed definitions. Strict per
    /// quest: one broken condition/action/enum drops the WHOLE quest with a report — a quest
    /// missing a clause must not run softened.
    /// </summary>
    public class QuestProvider(INarrativeConditionParser conditions, INarrativeActionParser actions) : IQuestProvider, IGameDataParticipant
    {
        private readonly Dictionary<string, QuestDefinition> _quests = [];

        public IReadOnlyCollection<QuestDefinition> All => _quests.Values;

        public IReadOnlyList<string> Catalogs => [DataCatalog.Quests];

        public void Apply(string catalog, GameDataFile file)
        {
            var data = JsonConvert.DeserializeObject<QuestsData>(file.Json)
                       ?? throw new InvalidOperationException($"Failed to deserialize quests file '{file.FileName}'");

            foreach (var entry in data.Quests)
            {
                var quest = ParseQuest(entry);
                if (quest != null) _quests[quest.Id] = quest;
            }
        }

        public QuestDefinition? Get(string questId)
        {
            if (_quests.TryGetValue(questId, out var quest)) return quest;
            Tracker.TrackNotFound($"Quest '{questId}' is not in the Quests catalog");
            return null;
        }

        private QuestDefinition? ParseQuest(QuestEntry entry)
        {
            if (entry.Id is not { Length: > 0 } || entry.Stages.Count == 0)
            {
                Tracker.TrackError($"Quest '{entry.Id}' is broken: id and at least one stage are required");
                return null;
            }

            try
            {
                var stages = new List<QuestStageDefinition>();
                foreach (var stage in entry.Stages)
                    stages.Add(ParseStage(entry.Id, stage));

                ValidateStageGraph(entry.Id, stages, entry.CanFail);

                return new QuestDefinition(
                    entry.Id,
                    entry.GiverNpcId,
                    entry.Faction == null ? null : EnumParser.ParseEnum<Fractions>(entry.Faction),
                    entry.Tier,
                    entry.Repeatable,
                    entry.TurnInNpcIds,
                    EnumParser.ParseEnum<DeclinePolicy>(entry.DeclinePolicy),
                    entry.DeclineCooldownHours,
                    entry.CanFail,
                    entry.TimeLimitHours,
                    ParseConditions(entry.AcceptConditions),
                    stages,
                    ParseRewards(entry.Rewards),
                    ParseActions(entry.OnAccept),
                    ParseActions(entry.OnDecline),
                    ParseActions(entry.OnFail));
            }
            catch (Exception exception)
            {
                Tracker.TrackError($"Quest '{entry.Id}' dropped: {exception.Message}");
                return null;
            }
        }

        private QuestStageDefinition ParseStage(string questId, QuestStageEntry stage)
        {
            var objectives = new List<QuestObjectiveDefinition>();
            foreach (var objective in stage.Objectives)
                objectives.Add(ParseObjective(questId, stage.Id, objective));

            if (objectives.Count == 0)
                throw new InvalidOperationException($"stage '{stage.Id}' has no objectives");

            return new QuestStageDefinition(stage.Id, objectives, ParseActions(stage.OnEnter), ParseActions(stage.OnComplete),
                ParseTransitions(stage), ParseOutcome(stage));
        }

        private List<QuestStageTransition> ParseTransitions(QuestStageEntry stage)
        {
            var transitions = new List<QuestStageTransition>();
            foreach (var transition in stage.Transitions)
            {
                if (transition.To.Length == 0)
                    throw new InvalidOperationException($"stage '{stage.Id}' has a transition without a target stage");
                transitions.Add(new QuestStageTransition(transition.To, ParseConditions(transition.Conditions)));
            }

            return transitions;
        }

        private QuestOutcomeDefinition? ParseOutcome(QuestStageEntry stage)
        {
            if (stage.Outcome is not { } outcome) return null;
            if (outcome.Id.Length == 0)
                throw new InvalidOperationException($"stage '{stage.Id}' has an outcome without an id");

            return new QuestOutcomeDefinition(outcome.Id, outcome.Fails, ParseRewards(outcome.Rewards ?? new QuestRewardsEntry()));
        }

        /// <summary>Routes have to lead somewhere and forward: a stage id used twice, an ending that
        /// also declares routes, a target no stage answers to, a duplicated outcome name (the save
        /// keeps the outcome by name), a failing ending on a quest that cannot fail or a stage
        /// reachable from itself drops the whole quest. A stage nothing leads to is only reported —
        /// unfinished authoring, not a broken quest.</summary>
        private static void ValidateStageGraph(string questId, List<QuestStageDefinition> stages, bool canFail)
        {
            var byId = new Dictionary<string, QuestStageDefinition>(StringComparer.Ordinal);
            foreach (var stage in stages)
            {
                if (stage.Id.Length == 0) throw new InvalidOperationException("a stage without an id");
                if (!byId.TryAdd(stage.Id, stage)) throw new InvalidOperationException($"two stages share the id '{stage.Id}'");
            }

            ValidateOutcomes(stages, canFail);
            foreach (var stage in stages)
                foreach (var transition in stage.Transitions)
                    if (!byId.ContainsKey(transition.ToStageId))
                        throw new InvalidOperationException($"stage '{stage.Id}' leads to the unknown stage '{transition.ToStageId}'");

            ValidateNoCycle(stages, byId);
            ReportUnreachableStages(questId, stages, byId);
        }

        private static void ValidateOutcomes(List<QuestStageDefinition> stages, bool canFail)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var stage in stages)
            {
                if (stage.Outcome == null) continue;
                if (stage.Transitions.Count > 0)
                    throw new InvalidOperationException($"stage '{stage.Id}' declares both an outcome and transitions: an ending leads nowhere");
                if (stage.Outcome.Fails && !canFail)
                    throw new InvalidOperationException($"stage '{stage.Id}' declares a failing outcome, but the quest cannot fail");
                if (!names.Add(stage.Outcome.Id))
                    throw new InvalidOperationException($"two stages end on the outcome '{stage.Outcome.Id}': the reward it pays would be ambiguous");
            }
        }

        private static void ValidateNoCycle(List<QuestStageDefinition> stages, Dictionary<string, QuestStageDefinition> byId)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var onPath = new HashSet<string>(StringComparer.Ordinal);

            void Walk(QuestStageDefinition stage)
            {
                if (!onPath.Add(stage.Id))
                    throw new InvalidOperationException($"stage '{stage.Id}' is reachable from itself: the quest would never end");
                if (visited.Add(stage.Id))
                    foreach (string next in Successors(stages, stage))
                        Walk(byId[next]);
                onPath.Remove(stage.Id);
            }

            foreach (var stage in stages)
                Walk(stage);
        }

        private static void ReportUnreachableStages(string questId, List<QuestStageDefinition> stages, Dictionary<string, QuestStageDefinition> byId)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal) { stages[0].Id };
            var pending = new Queue<QuestStageDefinition>([stages[0]]);
            while (pending.Count > 0)
                foreach (string next in Successors(stages, pending.Dequeue()))
                    if (reached.Add(next))
                        pending.Enqueue(byId[next]);

            var orphans = stages.Where(stage => !reached.Contains(stage.Id)).Select(stage => stage.Id).ToList();
            if (orphans.Count > 0)
                Tracker.TrackInfo($"Quest '{questId}': no route reaches the stages {string.Join(", ", orphans)}");
        }

        /// <summary>Stages a finished stage can lead to: its own routes, or the next of the list when it
        /// declares none. An ending leads nowhere.</summary>
        private static IEnumerable<string> Successors(List<QuestStageDefinition> stages, QuestStageDefinition stage)
        {
            if (stage.Outcome != null) return [];
            if (stage.Transitions.Count > 0) return stage.Transitions.Select(transition => transition.ToStageId);

            int next = stages.IndexOf(stage) + 1;
            return next < stages.Count ? [stages[next].Id] : [];
        }

        private QuestObjectiveDefinition ParseObjective(string questId, string stageId, QuestObjectiveEntry objective)
        {
            bool hasCondition = objective.Condition != null;
            bool hasCounter = objective.Counter != null && objective.Counter.Key.Length > 0;
            if (hasCondition == hasCounter)
                throw new InvalidOperationException($"objective '{stageId}/{objective.Id}' must have exactly one of condition/counter");

            INarrativeCondition? condition = null;
            if (hasCondition)
                condition = conditions.Parse(objective.Condition!)
                            ?? throw new InvalidOperationException($"objective '{stageId}/{objective.Id}' has a broken condition");

            var counter = objective.Counter == null
                ? null
                : new QuestCounter(objective.Counter.Key, objective.Counter.Amount, objective.Counter.Retroactive);

            return new QuestObjectiveDefinition(objective.Id, condition, counter, objective.Optional, objective.Hidden);
        }

        private QuestRewards ParseRewards(QuestRewardsEntry rewards)
        {
            var items = new List<QuestRewardItem>();
            foreach (var item in rewards.Items)
            {
                if (item.ItemId.Length == 0) throw new InvalidOperationException("reward item without itemId");
                items.Add(new QuestRewardItem(item.ItemId, item.Amount));
            }

            return new QuestRewards(rewards.InfluenceExp, items, ParseActions(rewards.Actions));
        }

        private List<INarrativeCondition> ParseConditions(Newtonsoft.Json.Linq.JToken? array)
        {
            var parsed = conditions.ParseList(array);
            if (array != null && parsed.Count != array.Count())
                throw new InvalidOperationException("a condition entry is broken");
            return parsed;
        }

        private List<INarrativeAction> ParseActions(Newtonsoft.Json.Linq.JToken? array)
        {
            var parsed = actions.ParseList(array);
            if (array != null && parsed.Count != array.Count())
                throw new InvalidOperationException("an action entry is broken");
            return parsed;
        }
    }
}
