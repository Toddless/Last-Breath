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
            if (entry.Id.Length == 0 || entry.Stages.Count == 0)
            {
                Tracker.TrackError($"Quest '{entry.Id}' is broken: id and at least one stage are required");
                return null;
            }

            try
            {
                var stages = new List<QuestStageDefinition>();
                foreach (var stage in entry.Stages)
                    stages.Add(ParseStage(entry.Id, stage));

                return new QuestDefinition(
                    entry.Id,
                    entry.GiverNpcId,
                    entry.Faction == null ? null : EnumParser.ParseEnum<Fractions>(entry.Faction),
                    entry.Tier,
                    entry.Repeatable,
                    entry.TurnInNpcIds,
                    EnumParser.ParseEnum<DeclinePolicy>(entry.DeclinePolicy),
                    entry.DeclineCooldownHours,
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

            return new QuestStageDefinition(stage.Id, objectives, ParseActions(stage.OnEnter), ParseActions(stage.OnComplete));
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
