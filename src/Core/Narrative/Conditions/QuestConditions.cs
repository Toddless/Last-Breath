namespace Core.Narrative.Conditions
{
    using System;
    using Data;
    using Newtonsoft.Json.Linq;
    using Quests;

    /// <summary>
    /// Quest-vocabulary conditions. All factories take Func-providers: the quest/dialogue data
    /// loaders own the parsers, so a direct IQuestLogService dependency here would close a DI
    /// cycle (QuestLogService → QuestProvider → parser → factory → QuestLogService).
    /// </summary>
    public class QuestStatusCondition(Func<IQuestLogService> log, string questId, QuestStatus? status) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => log().GetStatus(questId) == status;
    }

    public class QuestStatusConditionFactory(Func<IQuestLogService> log) : INarrativeConditionFactory
    {
        public string Type => "QuestStatus";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            string questId = json.Value<string>("questId") ?? string.Empty;
            string status = json.Value<string>("status") ?? string.Empty;
            if (questId.Length == 0 || status.Length == 0)
            {
                Tracker.TrackError("QuestStatus condition: questId and status are required");
                return null;
            }

            // "NotTaken" is the absence of a state, not a QuestStatus value.
            return new QuestStatusCondition(log, questId,
                status == "NotTaken" ? null : EnumParser.ParseEnum<QuestStatus>(status));
        }
    }

    public class CanAcceptQuestCondition(Func<IQuestLogService> log, string questId) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => log().CanAccept(questId, context);
    }

    public class CanAcceptQuestConditionFactory(Func<IQuestLogService> log) : INarrativeConditionFactory
    {
        public string Type => "CanAcceptQuest";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            RequireQuestId(json, "CanAcceptQuest") is { } questId ? new CanAcceptQuestCondition(log, questId) : null;

        internal static string? RequireQuestId(JObject json, string type)
        {
            string questId = json.Value<string>("questId") ?? string.Empty;
            if (questId.Length > 0) return questId;
            Tracker.TrackError($"{type} condition: questId is required");
            return null;
        }
    }

    public class CanTurnInQuestCondition(Func<IQuestLogService> log, string questId) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => log().CanTurnIn(questId);
    }

    public class CanTurnInQuestConditionFactory(Func<IQuestLogService> log) : INarrativeConditionFactory
    {
        public string Type => "CanTurnInQuest";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            CanAcceptQuestConditionFactory.RequireQuestId(json, "CanTurnInQuest") is { } questId
                ? new CanTurnInQuestCondition(log, questId)
                : null;
    }
}
