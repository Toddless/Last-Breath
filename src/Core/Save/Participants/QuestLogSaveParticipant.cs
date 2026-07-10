namespace Core.Save.Participants
{
    using System;
    using System.Linq;
    using Data.SaveData;
    using Narrative.Quests;
    using Newtonsoft.Json.Linq;

    public class QuestLogSaveParticipant(IQuestLogService questLog) : ISaveParticipant
    {
        public string SectionId => "quests";
        public int Version => 1;
        public int RestoreOrder => Save.RestoreOrder.Quests;

        public JToken Capture() => JToken.FromObject(new QuestLogSaveData
        {
            Quests = questLog.States.Select(state => new QuestStateSaveData
            {
                QuestId = state.QuestId,
                Status = state.Status.ToString(),
                StageIndex = state.StageIndex,
                CounterBaselines = new(state.CounterBaselines),
                GhostHintShown = state.GhostHintShown,
                AcceptedAtMinutes = state.AcceptedAtMinutes,
                NextOfferAtMinutes = state.NextOfferAtMinutes,
            }).ToList()
        });

        public void Restore(JToken data, int savedVersion)
        {
            var saved = data.ToObject<QuestLogSaveData>();
            if (saved == null) return;

            questLog.RestoreState(saved.Quests
                .Where(entry => Enum.TryParse<QuestStatus>(entry.Status, out _))
                .Select(entry =>
                {
                    var state = new QuestState(entry.QuestId)
                    {
                        Status = Enum.Parse<QuestStatus>(entry.Status),
                        StageIndex = entry.StageIndex,
                        GhostHintShown = entry.GhostHintShown,
                        AcceptedAtMinutes = entry.AcceptedAtMinutes,
                        NextOfferAtMinutes = entry.NextOfferAtMinutes,
                    };
                    foreach ((string key, int baseline) in entry.CounterBaselines)
                        state.CounterBaselines[key] = baseline;
                    return state;
                }));
        }
    }
}
