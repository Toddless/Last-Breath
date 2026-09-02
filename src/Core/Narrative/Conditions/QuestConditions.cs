namespace Core.Narrative.Conditions
{
    using System;
    using Data;
    using Data.GameData;
    using Newtonsoft.Json.Linq;
    using Quests;
    using Tooling.Schema.Model;

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
        private const string TypeName = "QuestStatus";
        private const string StatusKey = "status";
        private const string NotTakenStatus = "NotTaken";

        private static readonly RecordSchema s_parameters = NarrativeParameterSchema.Of(TypeName,
            QuestIdParameter.Field,
            NarrativeParameterSchema.Choice(StatusKey, required: true, [.. Enum.GetNames<QuestStatus>(), NotTakenStatus]));

        public string Type => TypeName;

        /// <summary>The offered statuses are the enum's plus the absence of one.</summary>
        public RecordSchema Parameters => s_parameters;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            string questId = json.Value<string>(QuestIdParameter.Key) ?? string.Empty;
            string status = json.Value<string>(StatusKey) ?? string.Empty;
            if (questId.Length == 0 || status.Length == 0)
            {
                Tracker.TrackError($"{TypeName} condition: {QuestIdParameter.Key} and {StatusKey} are required");
                return null;
            }

            // "NotTaken" is the absence of a state, not a QuestStatus value.
            return new QuestStatusCondition(log, questId,
                status == NotTakenStatus ? null : EnumParser.ParseEnum<QuestStatus>(status));
        }
    }

    public class CanAcceptQuestCondition(Func<IQuestLogService> log, string questId) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => log().CanAccept(questId, context);
    }

    public class CanAcceptQuestConditionFactory(Func<IQuestLogService> log) : INarrativeConditionFactory
    {
        private const string TypeName = "CanAcceptQuest";

        private static readonly RecordSchema s_parameters = NarrativeParameterSchema.Of(TypeName, QuestIdParameter.Field);

        public string Type => TypeName;

        public RecordSchema Parameters => s_parameters;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            QuestIdParameter.Require(json, Type) is { } questId ? new CanAcceptQuestCondition(log, questId) : null;
    }

    public class CanTurnInQuestCondition(Func<IQuestLogService> log, string questId) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => log().CanTurnIn(questId);
    }

    public class CanTurnInQuestConditionFactory(Func<IQuestLogService> log) : INarrativeConditionFactory
    {
        private const string TypeName = "CanTurnInQuest";

        private static readonly RecordSchema s_parameters = NarrativeParameterSchema.Of(TypeName, QuestIdParameter.Field);

        public string Type => TypeName;

        public RecordSchema Parameters => s_parameters;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser) =>
            QuestIdParameter.Require(json, Type) is { } questId ? new CanTurnInQuestCondition(log, questId) : null;
    }

    /// <summary>The quest reference the whole quest vocabulary is addressed by: one key, one field and one
    /// reading of it, so four conditions cannot drift apart over the same parameter.</summary>
    internal static class QuestIdParameter
    {
        public const string Key = "questId";

        public static readonly FieldSchema Field = NarrativeParameterSchema.Text(Key, required: true, DataCatalog.Quests);

        public static string? Require(JObject json, string type)
        {
            string questId = json.Value<string>(Key) ?? string.Empty;
            if (questId.Length > 0) return questId;

            Tracker.TrackError($"{type} condition: {Key} is required");
            return null;
        }
    }
}
