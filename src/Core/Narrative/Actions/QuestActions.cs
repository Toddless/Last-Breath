namespace Core.Narrative.Actions
{
    using System;
    using Conditions;
    using Newtonsoft.Json.Linq;
    using Quests;

    /// <summary>Quest-vocabulary actions (dialogue options are the normal path for all four).
    /// Func-injected log for the same DI-cycle reason as the quest conditions.</summary>
    public class QuestAction(Func<IQuestLogService> log, string questId, QuestActionKind kind) : INarrativeAction
    {
        public void Execute(NarrativeContext context)
        {
            var quests = log();
            switch (kind)
            {
                case QuestActionKind.Accept: quests.Accept(questId, context); break;
                case QuestActionKind.Decline: quests.Decline(questId, context); break;
                case QuestActionKind.TurnIn: quests.TurnIn(questId, context); break;
                case QuestActionKind.Abandon: quests.Abandon(questId); break;
            }
        }
    }

    public enum QuestActionKind : byte
    {
        Accept,
        Decline,
        TurnIn,
        Abandon,
    }

    public class QuestActionFactory(Func<IQuestLogService> log, QuestActionKind kind) : INarrativeActionFactory
    {
        public string Type => $"{kind}Quest";

        /// <summary>Built once per factory: all four kinds are addressed by the one quest reference the
        /// conditions read back, and only the name under which it is declared differs.</summary>
        public NarrativeRecordSpec Parameters => field ??= NarrativeParameterSchema.Of(Type, QuestIdParameter.Field);

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string questId = json.Value<string>(QuestIdParameter.Key) ?? string.Empty;
            if (questId.Length > 0) return new QuestAction(log, questId, kind);

            Tracker.TrackError($"{Type} action: {QuestIdParameter.Key} is required");
            return null;
        }
    }
}
